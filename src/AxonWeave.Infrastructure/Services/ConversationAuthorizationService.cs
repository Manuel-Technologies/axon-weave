using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AxonWeave.Infrastructure.Services;

public class ConversationAuthorizationService : IConversationAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;

    public ConversationAuthorizationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.ConversationParticipants.AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);
}
