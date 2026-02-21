using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Health;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.Pop3Host;
using Relate.Smtp.Pop3Host.Handlers;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure health check endpoint URL (avoids port conflict with other services)
var healthUrl = builder.Configuration["HealthCheck:Url"] ?? "http://localhost:8082";
builder.WebHost.UseUrls(healthUrl);

// Add infrastructure services (database + repositories)
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

// Register POP3 protocol health check
builder.Services.AddHealthChecks()
    .AddCheck<Pop3HealthCheck>("pop3", tags: ["protocol"]);

// Configure POP3 server options
builder.Services.Configure<Pop3ServerOptions>(
    builder.Configuration.GetSection("Pop3"));

// Register connection registry (singleton for tracking across all connections)
builder.Services.AddSingleton<Relate.Smtp.Core.Protocol.ConnectionRegistry>();

// Register handlers (scoped per connection)
builder.Services.AddScoped<Pop3UserAuthenticator>();
builder.Services.AddScoped<Pop3CommandHandler>();
builder.Services.AddScoped<Pop3MessageManager>();

// TLS certificate expiry health check
builder.Services.AddHealthChecks()
    .Add(new HealthCheckRegistration(
        "certificate",
        sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Pop3ServerOptions>>().Value;
            return new CertificateExpiryHealthCheck(options.CertificatePath, options.CertificatePassword);
        },
        failureStatus: HealthStatus.Degraded,
        tags: ["tls"]));

// Register hosted service
builder.Services.AddHostedService<Pop3ServerHostedService>();

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(builder.Configuration, "relate-mail-pop3");

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
