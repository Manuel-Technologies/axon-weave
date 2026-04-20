using AxonWeave.API.Abstractions;
using AxonWeave.API.Services;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Common;
using AxonWeave.Application.DTOs.Messages;
using AxonWeave.Domain.Entities;
using AxonWeave.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AxonWeave.API.Controllers;

/// <summary>
/// Reads, sends, deletes, and marks messages as read.
/// </summary>
[Authorize]
[Route("api/messages")]
public class MessageController : AuthenticatedControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationAuthorizationService _conversationAuthorizationService;
    private readonly IPresenceService _presenceService;
    private readonly IChatNotifier _chatNotifier;

    public MessageController(IUnitOfWork unitOfWork, IConversationAuthorizationService conversationAuthorizationService, IPresenceService presenceService, IChatNotifier chatNotifier)
    {
        _unitOfWork = unitOfWork;
        _conversationAuthorizationService = conversationAuthorizationService;
        _presenceService = presenceService;
        _chatNotifier = chatNotifier;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<MessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    /// <summary>
    /// Returns paginated messages for a conversation ordered from oldest to newest in the selected page.
    /// </summary>
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MessageDto>>>> Get([FromQuery] Guid conversationId, [FromQuery] DateTimeOffset? before, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetUserId();
        if (!await _conversationAuthorizationService.IsParticipantAsync(conversationId, currentUserId, cancellationToken))
        {
            return Forbid();
        }

        limit = Math.Clamp(limit, 1, 50);
        var query = _unitOfWork.Messages.Query()
            .AsNoTracking()
            .Include(x => x.Deliveries)
            .Where(x => x.ConversationId == conversationId);

        if (before.HasValue)
        {
            query = query.Where(x => x.CreatedAt < before.Value);
        }

        var messages = await query.OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IReadOnlyCollection<MessageDto>>
        {
            Data = messages.Select(x => x.ToDto()).ToList()
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    /// <summary>
    /// Sends a message to a conversation through the REST API.
    /// </summary>
    public async Task<ActionResult<ApiResponse<MessageDto>>> Send([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EncryptedContent) && string.IsNullOrWhiteSpace(request.MediaUrl))
        {
            return BadRequest(new { message = "A message requires encrypted content or a media URL." });
        }

        var currentUserId = GetUserId();
        if (!await _conversationAuthorizationService.IsParticipantAsync(request.ConversationId, currentUserId, cancellationToken))
        {
            return Forbid();
        }

        var participantIds = await _unitOfWork.ConversationParticipants.Query()
            .Where(x => x.ConversationId == request.ConversationId)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = currentUserId,
            EncryptedContent = request.EncryptedContent,
            MediaUrl = request.MediaUrl,
            MediaContentType = request.MediaContentType,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var participantId in participantIds.Where(x => x != currentUserId))
        {
            var isOnline = await _presenceService.IsOnlineAsync(participantId, cancellationToken);
            message.Deliveries.Add(new MessageDelivery
            {
                MessageId = message.Id,
                UserId = participantId,
                Status = isOnline ? MessageDeliveryStatus.Delivered : MessageDeliveryStatus.Failed,
                DeliveredAt = isOnline ? DateTimeOffset.UtcNow : null,
                UpdatedAt = DateTimeOffset.UtcNow,
                LastError = isOnline ? null : "Recipient offline at send time."
            });
        }

        await _unitOfWork.Messages.AddAsync(message, cancellationToken);

        var conversation = await _unitOfWork.Conversations.Query().FirstAsync(x => x.Id == request.ConversationId, cancellationToken);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        _unitOfWork.Conversations.Update(conversation);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedMessage = await _unitOfWork.Messages.Query()
            .Include(x => x.Deliveries)
            .FirstAsync(x => x.Id == message.Id, cancellationToken);

        var dto = savedMessage.ToDto();
        await _chatNotifier.BroadcastMessageAsync(request.ConversationId, dto, cancellationToken);

        return Ok(new ApiResponse<MessageDto> { Data = dto });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    /// <summary>
    /// Deletes a message for everyone in the conversation. Only the original sender can perform this action.
    /// </summary>
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId();
        var message = await _unitOfWork.Messages.Query()
            .Include(x => x.Deliveries)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (message is null)
        {
            return NotFound();
        }

        if (message.SenderId != currentUserId)
        {
            return Forbid();
        }

        message.IsDeletedForEveryone = true;
        message.DeletedAt = DateTimeOffset.UtcNow;
        message.EncryptedContent = "[deleted]";
        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _chatNotifier.BroadcastMessageAsync(message.ConversationId, message.ToDto(), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    /// <summary>
    /// Marks a message as read for the authenticated user.
    /// </summary>
    public async Task<ActionResult<ApiResponse<MessageDto>>> MarkRead(Guid id, [FromBody] MarkReadRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId();
        if (!await _conversationAuthorizationService.IsParticipantAsync(request.ConversationId, currentUserId, cancellationToken))
        {
            return Forbid();
        }

        var message = await _unitOfWork.Messages.Query()
            .Include(x => x.Deliveries)
            .FirstOrDefaultAsync(x => x.Id == id && x.ConversationId == request.ConversationId, cancellationToken);
        if (message is null)
        {
            return NotFound();
        }

        var delivery = message.Deliveries.FirstOrDefault(x => x.UserId == currentUserId);
        if (delivery is not null)
        {
            delivery.Status = MessageDeliveryStatus.Read;
            delivery.ReadAt = DateTimeOffset.UtcNow;
            delivery.UpdatedAt = DateTimeOffset.UtcNow;
        }

        message.ReadAt = DateTimeOffset.UtcNow;
        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _chatNotifier.BroadcastDeliveryReceiptAsync(request.ConversationId, [message.Id], currentUserId, cancellationToken);

        return Ok(new ApiResponse<MessageDto> { Data = message.ToDto() });
    }
}
