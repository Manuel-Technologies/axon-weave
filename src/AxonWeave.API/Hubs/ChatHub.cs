using AxonWeave.API.Models;
using AxonWeave.API.Services;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Messages;
using AxonWeave.Domain.Entities;
using AxonWeave.Domain.Enums;
using AxonWeave.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AxonWeave.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPresenceService _presenceService;
    private readonly IChatNotifier _notifier;

    public ChatHub(ApplicationDbContext dbContext, IPresenceService presenceService, IChatNotifier notifier)
    {
        _dbContext = dbContext;
        _presenceService = presenceService;
        _notifier = notifier;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await _presenceService.SetOnlineAsync(userId);

        var conversationIds = await _dbContext.ConversationParticipants
            .Where(x => x.UserId == userId)
            .Select(x => x.ConversationId)
            .ToListAsync();

        foreach (var conversationId in conversationIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        await _notifier.NotifyUserOnlineAsync(userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await _presenceService.SetOfflineAsync(userId);
        await _notifier.NotifyUserOfflineAsync(userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid conversationId, string encryptedContent)
    {
        if (string.IsNullOrWhiteSpace(encryptedContent))
        {
            throw new HubException("Encrypted content is required.");
        }

        var userId = GetUserId();
        var participantIds = await _dbContext.ConversationParticipants
            .Where(x => x.ConversationId == conversationId)
            .Select(x => x.UserId)
            .ToListAsync();

        if (!participantIds.Contains(userId))
        {
            throw new HubException("You are not a participant in this conversation.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            EncryptedContent = encryptedContent,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var participantId in participantIds.Where(id => id != userId))
        {
            var isOnline = await _presenceService.IsOnlineAsync(participantId);
            message.Deliveries.Add(new MessageDelivery
            {
                MessageId = message.Id,
                UserId = participantId,
                Status = isOnline ? MessageDeliveryStatus.Delivered : MessageDeliveryStatus.Failed,
                DeliveredAt = isOnline ? DateTimeOffset.UtcNow : null,
                LastError = isOnline ? null : "Recipient offline at send time.",
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        _dbContext.Messages.Add(message);

        var conversation = await _dbContext.Conversations.FirstAsync(x => x.Id == conversationId);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(message).Collection(x => x.Deliveries).LoadAsync();

        var dto = message.ToDto();
        await _notifier.BroadcastMessageAsync(conversationId, dto);
    }

    public async Task SendTyping(Guid conversationId, bool isTyping)
    {
        var userId = GetUserId();
        var isParticipant = await _dbContext.ConversationParticipants.AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId);
        if (!isParticipant)
        {
            throw new HubException("You are not a participant in this conversation.");
        }

        await _presenceService.RefreshAsync(userId);
        await _notifier.BroadcastTypingAsync(conversationId, userId, isTyping);
    }

    public async Task MarkDelivered(List<Guid> messageIds)
    {
        var userId = GetUserId();
        var deliveries = await _dbContext.MessageDeliveries
            .Include(x => x.Message)
            .Where(x => messageIds.Contains(x.MessageId) && x.UserId == userId)
            .ToListAsync();

        foreach (var delivery in deliveries)
        {
            delivery.Status = MessageDeliveryStatus.Delivered;
            delivery.DeliveredAt = DateTimeOffset.UtcNow;
            delivery.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        var grouped = deliveries.GroupBy(x => x.Message.ConversationId);
        foreach (var group in grouped)
        {
            await _notifier.BroadcastDeliveryReceiptAsync(group.Key, group.Select(x => x.MessageId), userId);
        }
    }

    public static string GetConversationGroupName(Guid conversationId) => $"conversation:{conversationId}";

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new HubException("Authenticated user id is missing.");
        }

        return userId;
    }
}
