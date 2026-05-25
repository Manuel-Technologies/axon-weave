using AxonWeave.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AxonWeave.API.Services;

public class StartupMigrationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupMigrationWorker> _logger;

    public StartupMigrationWorker(IServiceScopeFactory scopeFactory, ILogger<StartupMigrationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 10;

        for (var attempt = 1; attempt <= maxRetries && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var migrations = await dbContext.Database.GetMigrationsAsync(stoppingToken);
                if (migrations.Any())
                {
                    await dbContext.Database.MigrateAsync(stoppingToken);
                    _logger.LogInformation("Database migrations completed successfully.");
                }
                else
                {
                    await dbContext.Database.EnsureCreatedAsync(stoppingToken);
                    _logger.LogInformation("Database schema created successfully from the EF Core model because no migrations were found.");
                }

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Database migration attempt {Attempt} of {MaxRetries} failed. Retrying in the background...", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt * 3, 30)), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database migrations failed after all retries. The API will keep running, but database-backed routes will fail until connectivity is restored.");
            }
        }
    }
}
