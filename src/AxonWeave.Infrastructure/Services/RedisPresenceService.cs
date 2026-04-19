using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AxonWeave.Infrastructure.Services;

public class RedisPresenceService : IPresenceService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;

    public RedisPresenceService(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task SetOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(GetKey(userId), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), TimeSpan.FromSeconds(_options.PresenceTtlSeconds));
        }
        catch (RedisException)
        {
        }
    }

    public async Task SetOfflineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(GetKey(userId));
        }
        catch (RedisException)
        {
        }
    }

    public async Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(GetKey(userId));
        }
        catch (RedisException)
        {
            return false;
        }
    }

    public async Task RefreshAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyExpireAsync(GetKey(userId), TimeSpan.FromSeconds(_options.PresenceTtlSeconds));
        }
        catch (RedisException)
        {
        }
    }

    private static string GetKey(Guid userId) => $"presence:{userId}";
}
