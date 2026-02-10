using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Relate.Smtp.Infrastructure.Authentication;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;

namespace Relate.Smtp.Pop3Host.Handlers;

public class Pop3UserAuthenticator : ProtocolAuthenticator
{
    protected override string ProtocolName => "pop3";
    protected override string RequiredScope => "pop3";
    protected override ActivitySource ActivitySource => TelemetryConfiguration.Pop3ActivitySource;
    protected override Counter<long> AuthAttemptsCounter => ProtocolMetrics.Pop3AuthAttempts;
    protected override Counter<long> AuthFailuresCounter => ProtocolMetrics.Pop3AuthFailures;

    public Pop3UserAuthenticator(
        IServiceProvider serviceProvider,
        ILogger<Pop3UserAuthenticator> logger,
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
