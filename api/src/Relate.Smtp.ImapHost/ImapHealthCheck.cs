using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Relate.Smtp.ImapHost;

public class ImapHealthCheck : IHealthCheck
{
    private readonly ImapServerOptions _options;

    public ImapHealthCheck(IOptions<ImapServerOptions> options)
    {
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await client.ConnectAsync("localhost", _options.Port, cts.Token);

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            var greeting = await reader.ReadLineAsync(cts.Token);
            if (greeting == null || !greeting.StartsWith("* OK", StringComparison.Ordinal))
            {
                return HealthCheckResult.Unhealthy(
                    $"IMAP server on port {_options.Port} returned unexpected greeting: {greeting}");
            }

            await writer.WriteLineAsync("a001 LOGOUT".AsMemory(), cts.Token);

            return HealthCheckResult.Healthy(
                $"IMAP server accepting connections on port {_options.Port}");
        }
#pragma warning disable CA1031 // Do not catch general exception types - Health checks must report all failures as unhealthy
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy(
                $"IMAP server on port {_options.Port} is not responding", ex);
        }
    }
}
