using AxonWeave.API.Hubs;
using AxonWeave.API.Services;
using AxonWeave.Application;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using AxonWeave.Infrastructure;
using AxonWeave.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 5000;
    options.Limits.MaxConcurrentUpgradedConnections = 5000;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddHostedService<FailedMessageDeliveryWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 240,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod();

        if (corsOptions.AllowAnyOrigin || corsOptions.AllowedOrigins.Count == 0)
        {
            policy.SetIsOriginAllowed(_ => true);
        }
        else
        {
            policy.WithOrigins(corsOptions.AllowedOrigins.ToArray())
                .AllowCredentials();
        }
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.StreamBufferCapacity = 20;
}).AddJsonProtocol();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { }
});
app.Use((context, next) =>
{
    context.Request.Scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? context.Request.Scheme;
    return next();
});

await ApplyDatabaseMigrationsWithRetryAsync(app.Services, app.Logger);

var storageOptions = app.Services.GetRequiredService<IConfiguration>()
    .GetSection(StorageOptions.SectionName)
    .Get<StorageOptions>() ?? new StorageOptions();
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, storageOptions.RootPath);
Directory.CreateDirectory(uploadsPath);

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpLogging();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat", options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
});
app.MapGet("/", () => Results.Ok(new
{
    service = "axon-weave",
    status = "ok",
    docs = "/swagger",
    health = "/health",
    signalRHub = "/hubs/chat"
}));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (ApplicationDbContext dbContext, IConnectionMultiplexer redis, CancellationToken cancellationToken) =>
{
    var databaseOk = await dbContext.Database.CanConnectAsync(cancellationToken);
    var redisOk = redis.IsConnected;

    return databaseOk && redisOk
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Service is not ready.");
});

app.Run();

static async Task ApplyDatabaseMigrationsWithRetryAsync(IServiceProvider services, ILogger logger)
{
    const int maxRetries = 10;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Database migration attempt {Attempt} of {MaxRetries} failed. Retrying...", attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt * 3, 30)));
        }
    }

    using var finalScope = services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await finalDbContext.Database.MigrateAsync();
}
