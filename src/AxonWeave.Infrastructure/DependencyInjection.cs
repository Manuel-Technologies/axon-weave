using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using AxonWeave.Infrastructure.Persistence;
using AxonWeave.Infrastructure.Security;
using AxonWeave.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

namespace AxonWeave.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var postgresConnectionString = ResolvePostgresConnectionString(configuration);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IConversationAuthorizationService, ConversationAuthorizationService>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var configured = sp.GetRequiredService<IConfiguration>()["REDIS_URL"] ?? options.ConnectionString;
            return ConnectionMultiplexer.Connect(NormalizeRedisConnectionString(configured));
        });
        services.AddSingleton<IPresenceService, RedisPresenceService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static string ResolvePostgresConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return NormalizePostgresConnectionString(databaseUrl);
        }

        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return NormalizePostgresConnectionString(configured);
        }

        throw new InvalidOperationException("No PostgreSQL connection string was configured.");
    }

    private static string NormalizePostgresConnectionString(string value)
    {
        if (value.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
        var username = userInfo.ElementAtOrDefault(0) ?? string.Empty;
        var password = userInfo.ElementAtOrDefault(1) ?? string.Empty;
        var database = uri.AbsolutePath.Trim('/');

        return $"Host={uri.Host};Port={uri.Port};Database={database};Username={Uri.UnescapeDataString(username)};Password={Uri.UnescapeDataString(password)};SSL Mode=Require;Trust Server Certificate=true";
    }

    private static string NormalizeRedisConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        if (!uri.Scheme.StartsWith("redis", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
        var password = userInfo.Length == 2 ? userInfo[1] : userInfo.ElementAtOrDefault(0);
        var ssl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);

        return string.IsNullOrWhiteSpace(password)
            ? $"{uri.Host}:{uri.Port},ssl={ssl.ToString().ToLowerInvariant()},abortConnect=false"
            : $"{uri.Host}:{uri.Port},password={Uri.UnescapeDataString(password)},ssl={ssl.ToString().ToLowerInvariant()},abortConnect=false";
    }
}
