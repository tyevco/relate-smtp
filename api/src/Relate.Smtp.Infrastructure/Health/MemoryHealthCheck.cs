using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relate.Smtp.Infrastructure.Health;

public class MemoryHealthCheck : IHealthCheck
{
    private const long DegradedThresholdBytes = 1_500_000_000; // 1.5 GB
    private const long UnhealthyThresholdBytes = 1_900_000_000; // 1.9 GB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var process = Process.GetCurrentProcess();

        var workingSet = process.WorkingSet64;
        var heapSize = GC.GetTotalMemory(forceFullCollection: false);
        var totalAvailable = gcInfo.TotalAvailableMemoryBytes;

        var data = new Dictionary<string, object>
        {
            ["workingSetMB"] = workingSet / (1024.0 * 1024),
            ["gcHeapMB"] = heapSize / (1024.0 * 1024),
            ["totalAvailableMemoryMB"] = totalAvailable / (1024.0 * 1024),
            ["gen0Collections"] = GC.CollectionCount(0),
            ["gen1Collections"] = GC.CollectionCount(1),
            ["gen2Collections"] = GC.CollectionCount(2)
        };

        if (workingSet > UnhealthyThresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Working set {workingSet / (1024.0 * 1024):F0} MB exceeds {UnhealthyThresholdBytes / (1024.0 * 1024):F0} MB threshold",
                data: data));
        }

        if (workingSet > DegradedThresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Working set {workingSet / (1024.0 * 1024):F0} MB exceeds {DegradedThresholdBytes / (1024.0 * 1024):F0} MB threshold",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Working set {workingSet / (1024.0 * 1024):F0} MB, GC heap {heapSize / (1024.0 * 1024):F0} MB",
            data: data));
    }
}
