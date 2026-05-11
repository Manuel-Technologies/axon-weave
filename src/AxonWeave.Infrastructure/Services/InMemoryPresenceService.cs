using System.Collections.Concurrent;
using AxonWeave.Application.Common.Interfaces;

namespace AxonWeave.Infrastructure.Services;

public class InMemoryPresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _onlineUsers = new();
    private readonly TimeSpan _expiry = TimeSpan.FromMinutes(5);

    public Task SetOnlineAsync(Guid userId)
    {
        _onlineUsers[userId] = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task SetOfflineAsync(Guid userId)
    {
        _onlineUsers.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task RefreshAsync(Guid userId)
    {
        if (_onlineUsers.ContainsKey(userId))
        {
            _onlineUsers[userId] = DateTimeOffset.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<bool> IsOnlineAsync(Guid userId)
    {
        var isOnline = _onlineUsers.TryGetValue(userId, out var lastSeen) && 
                       DateTimeOffset.UtcNow - lastSeen <= _expiry;
        return Task.FromResult(isOnline);
    }
}