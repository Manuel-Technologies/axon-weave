using AxonWeave.Domain.Entities;

namespace AxonWeave.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<PendingOtp> PendingOtps { get; }
    IRepository<Conversation> Conversations { get; }
    IRepository<ConversationParticipant> ConversationParticipants { get; }
    IRepository<Message> Messages { get; }
    IRepository<MessageDelivery> MessageDeliveries { get; }
    IRepository<MediaAsset> MediaAssets { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
