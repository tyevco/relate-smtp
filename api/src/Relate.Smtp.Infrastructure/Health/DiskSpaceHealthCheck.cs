using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relate.Smtp.Infrastructure.Health;

public class DiskSpaceHealthCheck : IHealthCheck
{
    private const long DegradedThresholdBytes = 1L * 1024 * 1024 * 1024; // 1 GB
    private const long UnhealthyThresholdBytes = 100L * 1024 * 1024; // 100 MB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo("/");
            var available = drive.AvailableFreeSpace;
            var total = drive.TotalSize;
            var usedPercent = (double)(total - available) / total * 100;

            var data = new Dictionary<string, object>
            {
                ["availableGB"] = available / (1024.0 * 1024 * 1024),
                ["totalGB"] = total / (1024.0 * 1024 * 1024),
                ["usedPercent"] = usedPercent
            };

            if (available < UnhealthyThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Disk space critically low: {available / (1024.0 * 1024):F0} MB available ({usedPercent:F1}% used)",
                    data: data));
            }

            if (available < DegradedThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Disk space low: {available / (1024.0 * 1024 * 1024):F2} GB available ({usedPercent:F1}% used)",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"{available / (1024.0 * 1024 * 1024):F2} GB available ({usedPercent:F1}% used)",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to read disk space", ex));
        }
    }
}
