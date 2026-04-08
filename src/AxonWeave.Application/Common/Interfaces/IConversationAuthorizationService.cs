namespace AxonWeave.Application.Common.Interfaces;

public interface IConversationAuthorizationService
{
    Task<bool> IsParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
