using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Domain.Entities;

namespace AxonWeave.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        Users = new Repository<User>(dbContext);
        PendingOtps = new Repository<PendingOtp>(dbContext);
        Conversations = new Repository<Conversation>(dbContext);
        ConversationParticipants = new Repository<ConversationParticipant>(dbContext);
        Messages = new Repository<Message>(dbContext);
        MessageDeliveries = new Repository<MessageDelivery>(dbContext);
        MediaAssets = new Repository<MediaAsset>(dbContext);
    }

    public IRepository<User> Users { get; }
    public IRepository<PendingOtp> PendingOtps { get; }
    public IRepository<Conversation> Conversations { get; }
    public IRepository<ConversationParticipant> ConversationParticipants { get; }
    public IRepository<Message> Messages { get; }
    public IRepository<MessageDelivery> MessageDeliveries { get; }
    public IRepository<MediaAsset> MediaAssets { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
