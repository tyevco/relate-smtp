using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Health;

public class DeliveryQueueHealthCheck : IHealthCheck
{
    private const int StalledThresholdMinutes = 10;
    private const int MaxRetryWarning = 3;
    private const int QueueBacklogWarning = 100;

    private readonly AppDbContext _dbContext;

    public DeliveryQueueHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stalledCutoff = DateTimeOffset.UtcNow.AddMinutes(-StalledThresholdMinutes);

            var stalledCount = await _dbContext.OutboundEmails
                .CountAsync(e => e.Status == OutboundEmailStatus.Sending
                    && e.QueuedAt < stalledCutoff, cancellationToken);

            var highRetryCount = await _dbContext.OutboundEmails
                .CountAsync(e => (e.Status == OutboundEmailStatus.Queued || e.Status == OutboundEmailStatus.Sending)
                    && e.RetryCount >= MaxRetryWarning, cancellationToken);

            var queuedCount = await _dbContext.OutboundEmails
                .CountAsync(e => e.Status == OutboundEmailStatus.Queued, cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["stalledCount"] = stalledCount,
                ["highRetryCount"] = highRetryCount,
                ["queuedCount"] = queuedCount
            };

            if (stalledCount > 0)
            {
                return HealthCheckResult.Unhealthy(
                    $"{stalledCount} emails stalled in Sending status for over {StalledThresholdMinutes} minutes",
                    data: data);
            }

            if (highRetryCount > 0 || queuedCount > QueueBacklogWarning)
            {
                var reasons = new List<string>();
                if (highRetryCount > 0)
                    reasons.Add($"{highRetryCount} emails with {MaxRetryWarning}+ retries");
                if (queuedCount > QueueBacklogWarning)
                    reasons.Add($"queue backlog of {queuedCount}");

                return HealthCheckResult.Degraded(
                    string.Join("; ", reasons),
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"{queuedCount} emails queued, no stalled deliveries",
                data: data);
        }
#pragma warning disable CA1031 // Do not catch general exception types - Health checks must report all failures as unhealthy
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy(
                "Failed to query delivery queue status", ex);
        }
    }
}
