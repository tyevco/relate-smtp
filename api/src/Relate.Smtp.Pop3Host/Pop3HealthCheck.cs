using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Relate.Smtp.Pop3Host;

public class Pop3HealthCheck : IHealthCheck
{
    private readonly Pop3ServerOptions _options;

    public Pop3HealthCheck(IOptions<Pop3ServerOptions> options)
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
            if (greeting == null || !greeting.StartsWith("+OK"))
            {
                return HealthCheckResult.Unhealthy(
                    $"POP3 server on port {_options.Port} returned unexpected greeting: {greeting}");
            }

            await writer.WriteLineAsync("QUIT".AsMemory(), cts.Token);

            return HealthCheckResult.Healthy(
                $"POP3 server accepting connections on port {_options.Port}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"POP3 server on port {_options.Port} is not responding", ex);
        }
    }
}
