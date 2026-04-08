using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Domain.Enums;
using AxonWeave.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AxonWeave.API.Services;

public class FailedMessageDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FailedMessageDeliveryWorker> _logger;

    public FailedMessageDeliveryWorker(IServiceScopeFactory scopeFactory, ILogger<FailedMessageDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();
                var notifier = scope.ServiceProvider.GetRequiredService<IChatNotifier>();

                var deliveries = await dbContext.MessageDeliveries
                    .Include(x => x.Message)
                    .Where(x => x.Status == MessageDeliveryStatus.Failed && x.RetryCount < 10)
                    .OrderBy(x => x.UpdatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var delivery in deliveries)
                {
                    if (await presenceService.IsOnlineAsync(delivery.UserId, stoppingToken))
                    {
                        delivery.Status = MessageDeliveryStatus.Delivered;
                        delivery.DeliveredAt = DateTimeOffset.UtcNow;
                        delivery.UpdatedAt = DateTimeOffset.UtcNow;
                        await notifier.BroadcastDeliveryReceiptAsync(delivery.Message.ConversationId, [delivery.MessageId], delivery.UserId, stoppingToken);
                    }
                    else
                    {
                        delivery.RetryCount += 1;
                        delivery.UpdatedAt = DateTimeOffset.UtcNow;
                        delivery.LastError = "User still offline during retry cycle.";
                    }
                }

                if (deliveries.Count > 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process pending message deliveries.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
