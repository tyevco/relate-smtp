using System.Diagnostics;
using System.Diagnostics.Metrics;
using SmtpServer;
using SmtpServer.Authentication;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Infrastructure.Authentication;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;

namespace Relate.Smtp.SmtpHost.Handlers;

public class CustomUserAuthenticator : ProtocolAuthenticator, IUserAuthenticator
{
    protected override string ProtocolName => "smtp";
    protected override string RequiredScope => "smtp";
    protected override ActivitySource ActivitySource => TelemetryConfiguration.SmtpActivitySource;
    protected override Counter<long> AuthAttemptsCounter => ProtocolMetrics.SmtpAuthAttempts;
    protected override Counter<long> AuthFailuresCounter => ProtocolMetrics.SmtpAuthFailures;

    public CustomUserAuthenticator(
        IServiceProvider serviceProvider,
        ILogger<CustomUserAuthenticator> logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IAuthenticationRateLimiter rateLimiter)
        : base(serviceProvider, logger, backgroundTaskQueue, rateLimiter)
    {
    }

    public async Task<bool> AuthenticateAsync(
        ISessionContext context,
        string user,
        string password,
        CancellationToken cancellationToken)
    {
        var clientIp = context.Properties.TryGetValue("ClientIP", out var ip) ? ip?.ToString() ?? "unknown" : "unknown";

        var (authenticated, userId) = await AuthenticateCoreAsync(user, password, clientIp, cancellationToken);

        if (authenticated && userId.HasValue)
        {
            // Store authenticated user info in session context for later use
            context.Properties["AuthenticatedUserId"] = userId.Value;
            context.Properties["AuthenticatedEmail"] = user.ToLowerInvariant();
        }

        return authenticated;
    }
}
