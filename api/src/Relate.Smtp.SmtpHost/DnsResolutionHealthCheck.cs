using DnsClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relate.Smtp.SmtpHost;

public class DnsResolutionHealthCheck : IHealthCheck
{
    private const string TestDomain = "gmail.com";

    private readonly ILookupClient _lookupClient;

    public DnsResolutionHealthCheck(ILookupClient lookupClient)
    {
        _lookupClient = lookupClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _lookupClient.QueryAsync(
                TestDomain, DnsClient.QueryType.MX, cancellationToken: cancellationToken);

            var mxRecords = result.Answers
                .OfType<DnsClient.Protocol.MxRecord>()
                .ToList();

            if (mxRecords.Count > 0)
            {
                var hosts = string.Join(", ", mxRecords
                    .OrderBy(mx => mx.Preference)
                    .Take(3)
                    .Select(mx => mx.Exchange.Value.TrimEnd('.')));

                return HealthCheckResult.Healthy(
                    $"DNS MX resolution working ({TestDomain} → {hosts})",
                    data: new Dictionary<string, object>
                    {
                        ["testDomain"] = TestDomain,
                        ["mxRecordCount"] = mxRecords.Count
                    });
            }

            return HealthCheckResult.Degraded(
                $"No MX records returned for {TestDomain}",
                data: new Dictionary<string, object>
                {
                    ["testDomain"] = TestDomain,
                    ["mxRecordCount"] = 0
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "DNS MX resolution failed", ex);
        }
    }
}
