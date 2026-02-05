using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Relate.Smtp.Infrastructure.Telemetry;

public static class TelemetryConfiguration
{
    public const string ServiceName = "relate-mail";

    // Activity sources for custom spans
    public static readonly ActivitySource SmtpActivitySource = new("Relate.Smtp", "1.0.0");
    public static readonly ActivitySource Pop3ActivitySource = new("Relate.Pop3", "1.0.0");
    public static readonly ActivitySource ImapActivitySource = new("Relate.Imap", "1.0.0");
    public static readonly ActivitySource ApiActivitySource = new("Relate.Api", "1.0.0");

    public static IServiceCollection AddRelateTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        var otlpEndpoint = configuration["Otel:Endpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(SmtpActivitySource.Name)
                    .AddSource(Pop3ActivitySource.Name)
                    .AddSource(ImapActivitySource.Name)
                    .AddSource(ApiActivitySource.Name)
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                    });

                // Allow the caller to add additional instrumentation (e.g., ASP.NET Core, HTTP client)
                configureTracing?.Invoke(tracing);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts =>
                        opts.Endpoint = new Uri(otlpEndpoint));
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ProtocolMetrics.SmtpMeterName)
                    .AddMeter(ProtocolMetrics.Pop3MeterName)
                    .AddMeter(ProtocolMetrics.ImapMeterName);

                // Allow the caller to add additional instrumentation (e.g., ASP.NET Core, HTTP client)
                configureMetrics?.Invoke(metrics);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts =>
                        opts.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}
