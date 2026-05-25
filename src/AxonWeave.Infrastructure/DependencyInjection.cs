using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.Options;
using AxonWeave.Infrastructure.Persistence;
using AxonWeave.Infrastructure.Security;
using AxonWeave.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var databaseUrl = configuration["DATABASE_URL"];
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? configuration["Postgres__ConnectionString"]
            ?? ConvertRenderPostgresUrl(databaseUrl);
        var sqliteConnectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=axon_weave.db";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(postgresConnectionString))
            {
                options.UseNpgsql(postgresConnectionString);
            }
            else
            {
                options.UseSqlite(sqliteConnectionString);
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IConversationAuthorizationService, ConversationAuthorizationService>();

        var redisConnectionString = ConvertRedisUrl(configuration["REDIS_URL"])
            ?? configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()?.ConnectionString;
        if (!string.IsNullOrWhiteSpace(redisConnectionString) && !redisConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
                redisOptions.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(redisOptions);
            });
            services.AddSingleton<IPresenceService, RedisPresenceService>();
        }
        else
        {
            services.AddSingleton<IPresenceService, InMemoryPresenceService>();
        }

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

    private static string? ConvertRenderPostgresUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        {
            return databaseUrl;
        }

        if (uri.Scheme is not ("postgres" or "postgresql"))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
        var database = uri.AbsolutePath.TrimStart('/');

        return new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = Npgsql.SslMode.Require,
            TrustServerCertificate = true,
            Pooling = true
        }.ConnectionString;
    }

    private static string? ConvertRedisUrl(string? redisUrl)
    {
        if (string.IsNullOrWhiteSpace(redisUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(redisUrl, UriKind.Absolute, out var uri))
        {
            return redisUrl;
        }

        if (uri.Scheme is not ("redis" or "rediss"))
        {
            return redisUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? userInfo.ElementAtOrDefault(0) ?? string.Empty);
        var parts = new List<string>
        {
            $"{uri.Host}:{(uri.Port > 0 ? uri.Port : 6379)}",
            "abortConnect=false"
        };

        if (uri.Scheme == "rediss")
        {
            parts.Add("ssl=true");
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            parts.Add($"password={password}");
        }

        return string.Join(",", parts);
    }
}
