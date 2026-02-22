using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Health;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.SmtpHost;
using Relate.Smtp.SmtpHost.Services;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure health check endpoint URL (avoids port conflict with other services)
var healthUrl = builder.Configuration["HealthCheck:Url"] ?? "http://localhost:8081";
builder.WebHost.UseUrls(healthUrl);

// Add infrastructure services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is required. Set via environment variable ConnectionStrings__DefaultConnection.");
    }
    connectionString = "Host=localhost;Port=5432;Database=relate_mail;Username=postgres;Password=postgres";
    Console.WriteLine("WARNING: Using default development database connection. Set ConnectionStrings__DefaultConnection for production.");
}

builder.Services.AddInfrastructure(connectionString);

// Register SMTP-specific health checks
builder.Services.AddHealthChecks()
    .AddCheck<SmtpHealthCheck>("smtp", tags: ["protocol"])
    .AddCheck<DnsResolutionHealthCheck>("dns-mx", tags: ["smtp"]);

// Configure HTTP client for notification service
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000";
var internalApiKey = builder.Configuration["Internal:ApiKey"];

builder.Services.AddHttpClient<IEmailNotificationService, HttpEmailNotificationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    if (!string.IsNullOrEmpty(internalApiKey))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("ApiKey", internalApiKey);
    }
});

// Configure SMTP server options
builder.Services.Configure<SmtpServerOptions>(
    builder.Configuration.GetSection("Smtp"));

// TLS certificate expiry health check
builder.Services.AddHealthChecks()
    .Add(new HealthCheckRegistration(
        "certificate",
        sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmtpServerOptions>>().Value;
            return new CertificateExpiryHealthCheck(options.CertificatePath, options.CertificatePassword);
        },
        failureStatus: HealthStatus.Degraded,
        tags: ["tls"]));

// Add SMTP server hosted service
builder.Services.AddHostedService<SmtpServerHostedService>();

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(builder.Configuration, "relate-mail-smtp");

var app = builder.Build();

// Health check endpoint
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.Run();

// Prevent CS0433 ambiguity: keep Program internal so test projects
// only see the API's public Program for WebApplicationFactory<Program>.
internal partial class Program;
