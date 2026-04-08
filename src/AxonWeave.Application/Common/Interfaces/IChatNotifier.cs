using AxonWeave.Application.DTOs.Messages;

namespace AxonWeave.Application.Common.Interfaces;

public interface IChatNotifier
{
    Task BroadcastMessageAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default);
    Task BroadcastTypingAsync(Guid conversationId, Guid userId, bool isTyping, CancellationToken cancellationToken = default);
    Task NotifyUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task NotifyUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task BroadcastDeliveryReceiptAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid recipientUserId, CancellationToken cancellationToken = default);
}
