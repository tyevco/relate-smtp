using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Infrastructure.Authentication;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;

namespace Relate.Smtp.ImapHost.Handlers;

public class ImapUserAuthenticator : ProtocolAuthenticator
{
    protected override string ProtocolName => "imap";
    protected override string RequiredScope => "imap";
    protected override ActivitySource ActivitySource => TelemetryConfiguration.ImapActivitySource;
    protected override Counter<long> AuthAttemptsCounter => ProtocolMetrics.ImapAuthAttempts;
    protected override Counter<long> AuthFailuresCounter => ProtocolMetrics.ImapAuthFailures;

    public ImapUserAuthenticator(
        IServiceProvider serviceProvider,
        ILogger<ImapUserAuthenticator> logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IAuthenticationRateLimiter rateLimiter)
        : base(serviceProvider, logger, backgroundTaskQueue, rateLimiter)
    {
    }

    public Task<(bool IsAuthenticated, Guid? UserId)> AuthenticateAsync(
        string username,
        string password,
        string clientIp,
        CancellationToken ct)
        => AuthenticateCoreAsync(username, password, clientIp, ct);
}
