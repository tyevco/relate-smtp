using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relate.Smtp.Infrastructure.Data;

namespace Relate.Smtp.Infrastructure.Health;

public class ConnectionPoolHealthCheck : IHealthCheck
{
    private const int DegradedThreshold = 80;
    private const int UnhealthyThreshold = 95;

    private readonly AppDbContext _dbContext;

    public ConnectionPoolHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Query PostgreSQL for active connections to our database
            var activeConnections = await _dbContext.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*)::int AS \"Value\" FROM pg_stat_activity WHERE datname = current_database()")
                .FirstAsync(cancellationToken);

            var maxConnections = await _dbContext.Database
                .SqlQueryRaw<int>(
                    "SELECT setting::int AS \"Value\" FROM pg_settings WHERE name = 'max_connections'")
                .FirstAsync(cancellationToken);

            var usedPercent = maxConnections > 0 ? (double)activeConnections / maxConnections * 100 : 0;

            var data = new Dictionary<string, object>
            {
                ["activeConnections"] = activeConnections,
                ["maxConnections"] = maxConnections,
                ["usedPercent"] = usedPercent
            };

            if (usedPercent > UnhealthyThreshold)
            {
                return HealthCheckResult.Unhealthy(
                    $"Connection pool near exhaustion: {activeConnections}/{maxConnections} ({usedPercent:F1}%)",
                    data: data);
            }

            if (usedPercent > DegradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"Connection pool pressure: {activeConnections}/{maxConnections} ({usedPercent:F1}%)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"{activeConnections}/{maxConnections} connections ({usedPercent:F1}%)",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to query connection pool statistics", ex);
        }
    }
}
