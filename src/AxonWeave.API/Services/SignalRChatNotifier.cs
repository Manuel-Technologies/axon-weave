using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Messages;
using AxonWeave.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AxonWeave.API.Services;

public class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }
//
    public Task BroadcastMessageAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(ChatHub.GetConversationGroupName(conversationId)).SendAsync("OnMessageReceived", message, cancellationToken);

    public Task BroadcastTypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(ChatHub.GetConversationGroupName(conversationId)).SendAsync("OnTyping", new { conversationId, userId, isTyping }, cancellationToken);

    public Task NotifyUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.All.SendAsync("OnUserOnline", new { userId }, cancellationToken);

    public Task NotifyUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.All.SendAsync("OnUserOffline", new { userId }, cancellationToken);

    public Task BroadcastDeliveryReceiptAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid recipientUserId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(ChatHub.GetConversationGroupName(conversationId)).SendAsync("OnDeliveryReceipt", new { conversationId, recipientUserId, messageIds }, cancellationToken);
}
