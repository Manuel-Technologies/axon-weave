using AxonWeave.API.Abstractions;
using AxonWeave.API.Hubs;
using AxonWeave.API.Services;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Common;
using AxonWeave.Application.DTOs.Conversations;
using AxonWeave.Domain.Entities;
using AxonWeave.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AxonWeave.API.Controllers;

[Authorize]
[Route("api/conversations")]
public class ConversationsController : AuthenticatedControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ConversationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId();
        var participantIds = request.ParticipantIds.Distinct().ToList();
        if (!participantIds.Contains(currentUserId))
        {
            participantIds.Add(currentUserId);
        }

        if (!request.IsGroup && participantIds.Count != 2)
        {
            return BadRequest(new { message = "Direct conversations must contain exactly two participants." });
        }

        if (request.IsGroup && string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Group conversations require a title." });
        }

        var users = await _unitOfWork.Users.Query()
            .Where(x => participantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (users.Count != participantIds.Count)
        {
            return BadRequest(new { message = "One or more participants were not found." });
        }

        if (!request.IsGroup)
        {
            var existingDirectConversation = await _unitOfWork.Conversations.Query()
                .Include(x => x.Participants).ThenInclude(x => x.User)
                .Include(x => x.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(x => x.Type == ConversationType.Direct && x.Participants.Count == 2)
                .FirstOrDefaultAsync(x =>
                    x.Participants.All(p => participantIds.Contains(p.UserId)) &&
                    participantIds.All(id => x.Participants.Select(p => p.UserId).Contains(id)),
                    cancellationToken);

            if (existingDirectConversation is not null)
            {
                return Ok(new ApiResponse<ConversationDto> { Data = existingDirectConversation.ToDto() });
            }
        }

        var conversation = new Conversation
        {
            Title = request.IsGroup ? (request.Title?.Trim() ?? "New Group") : string.Empty,
            Type = request.IsGroup ? ConversationType.Group : ConversationType.Direct,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        foreach (var userId in participantIds)
        {
            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        await _unitOfWork.Conversations.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedConversation = await _unitOfWork.Conversations.Query()
            .Include(x => x.Participants).ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .FirstAsync(x => x.Id == conversation.Id, cancellationToken);

        return Ok(new ApiResponse<ConversationDto> { Data = savedConversation.ToDto() });
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ConversationDto>>>> List(CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId();
        var conversations = await _unitOfWork.Conversations.Query()
            .Include(x => x.Participants).ThenInclude(x => x.User)
            .Include(x => x.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .Where(x => x.Participants.Any(p => p.UserId == currentUserId))
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IReadOnlyCollection<ConversationDto>>
        {
            Data = conversations.Select(x => x.ToDto()).ToList()
        });
    }
}
