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

        return services;
    }
}
