using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relate.Smtp.Api.Hubs;

namespace Relate.Smtp.Api.Health;

public class SignalRHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _services;

    public SignalRHealthCheck(IServiceProvider services)
    {
        _services = services;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var hubContext = _services.GetService<IHubContext<EmailHub>>();
            if (hubContext == null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "SignalR EmailHub is not registered in the service container"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "SignalR EmailHub is available"));
        }
#pragma warning disable CA1031 // Do not catch general exception types - Health checks must report all failures as unhealthy
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to resolve SignalR EmailHub", ex));
        }
    }
}
