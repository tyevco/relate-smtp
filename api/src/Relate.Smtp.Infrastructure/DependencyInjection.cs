using DnsClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;
using Relate.Smtp.Infrastructure.Repositories;
using Relate.Smtp.Infrastructure.Services;

namespace Relate.Smtp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISmtpApiKeyRepository, SmtpApiKeyRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IEmailLabelRepository, EmailLabelRepository>();
        services.AddScoped<IEmailFilterRepository, EmailFilterRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<IOutboundEmailRepository, OutboundEmailRepository>();

        // Background task queue for non-critical updates (e.g., LastUsedAt)
        services.AddSingleton<BackgroundTaskQueue>();
        services.AddSingleton<IBackgroundTaskQueue>(sp => sp.GetRequiredService<BackgroundTaskQueue>());
        services.AddHostedService<BackgroundTaskQueueHostedService>();

        // Authentication rate limiter for brute force protection
        services.AddSingleton<IAuthenticationRateLimiter, AuthenticationRateLimiter>();

        // Outbound mail delivery services
        services.AddSingleton<ILookupClient>(new LookupClient());
        services.AddSingleton<MxResolverService>();
        services.AddScoped<SmtpDeliveryService>();
        services.AddHostedService<DeliveryQueueProcessor>();

        return services;
    }
}
