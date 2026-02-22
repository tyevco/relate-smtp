using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Relate.Smtp.SmtpHost;

public class SmtpHealthCheck : IHealthCheck
{
    private readonly SmtpServerOptions _options;

    public SmtpHealthCheck(IOptions<SmtpServerOptions> options)
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
            if (greeting == null || !greeting.StartsWith("220", StringComparison.Ordinal))
            {
                return HealthCheckResult.Unhealthy(
                    $"SMTP server on port {_options.Port} returned unexpected greeting: {greeting}");
            }

            await writer.WriteLineAsync("QUIT".AsMemory(), cts.Token);

            return HealthCheckResult.Healthy(
                $"SMTP server accepting connections on port {_options.Port}");
        }
#pragma warning disable CA1031 // Do not catch general exception types - Health checks must report all failures as unhealthy
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy(
                $"SMTP server on port {_options.Port} is not responding", ex);
        }
    }
}
