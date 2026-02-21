using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relate.Smtp.Infrastructure.Health;

public class CertificateExpiryHealthCheck : IHealthCheck
{
    private const int WarningDays = 30;
    private const int CriticalDays = 7;

    private readonly string? _certificatePath;
    private readonly string? _certificatePassword;

    public CertificateExpiryHealthCheck(string? certificatePath, string? certificatePassword)
    {
        _certificatePath = certificatePath;
        _certificatePassword = certificatePassword;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_certificatePath))
        {
            return Task.FromResult(HealthCheckResult.Healthy("No TLS certificate configured"));
        }

        try
        {
            if (!File.Exists(_certificatePath))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Certificate file not found: {_certificatePath}"));
            }

            using var cert = string.IsNullOrEmpty(_certificatePassword)
                ? X509CertificateLoader.LoadCertificateFromFile(_certificatePath)
                : X509CertificateLoader.LoadPkcs12FromFile(_certificatePath, _certificatePassword);

            var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).TotalDays;

            var data = new Dictionary<string, object>
            {
                ["subject"] = cert.Subject,
                ["notBefore"] = cert.NotBefore.ToString("O"),
                ["notAfter"] = cert.NotAfter.ToString("O"),
                ["daysUntilExpiry"] = daysUntilExpiry,
                ["thumbprint"] = cert.Thumbprint
            };

            if (daysUntilExpiry < 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Certificate expired {Math.Abs(daysUntilExpiry):F0} days ago ({cert.NotAfter:d})",
                    data: data));
            }

            if (daysUntilExpiry < CriticalDays)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Certificate expires in {daysUntilExpiry:F0} days ({cert.NotAfter:d})",
                    data: data));
            }

            if (daysUntilExpiry < WarningDays)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Certificate expires in {daysUntilExpiry:F0} days ({cert.NotAfter:d})",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Certificate valid for {daysUntilExpiry:F0} days (expires {cert.NotAfter:d})",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Failed to load certificate from {_certificatePath}", ex));
        }
    }
}
