using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Relate.Smtp.Core.Interfaces;
using Relate.Smtp.Infrastructure.Data;
using Relate.Smtp.Infrastructure.Repositories;

namespace Relate.Smtp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (IsPostgreSqlConnectionString(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISmtpApiKeyRepository, SmtpApiKeyRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IEmailLabelRepository, EmailLabelRepository>();
        services.AddScoped<IEmailFilterRepository, EmailFilterRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

        return services;
    }

    private static bool IsPostgreSqlConnectionString(string connectionString)
    {
        var lowerConnectionString = connectionString.ToLowerInvariant();
        return lowerConnectionString.Contains("host=") ||
               lowerConnectionString.Contains("server=") && (
                   lowerConnectionString.Contains("port=5432") ||
                   lowerConnectionString.Contains("database=") ||
                   lowerConnectionString.Contains("user id=") ||
                   lowerConnectionString.Contains("username="));
    }
}
