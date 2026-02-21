using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.ImapHost;
using Relate.Smtp.ImapHost.Handlers;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure health check endpoint URL (avoids port conflict with other services)
var healthUrl = builder.Configuration["HealthCheck:Url"] ?? "http://localhost:8083";
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

// Register IMAP protocol health check
builder.Services.AddHealthChecks()
    .AddCheck<ImapHealthCheck>("imap", tags: ["protocol"]);

// Configure IMAP server options
builder.Services.Configure<ImapServerOptions>(
    builder.Configuration.GetSection("Imap"));

// Register connection registry (singleton for tracking across all connections)
builder.Services.AddSingleton<Relate.Smtp.Core.Protocol.ConnectionRegistry>();

// Register handlers (scoped per connection)
builder.Services.AddScoped<ImapUserAuthenticator>();
builder.Services.AddScoped<ImapCommandHandler>();
builder.Services.AddScoped<ImapMessageManager>();

// Register hosted service
builder.Services.AddHostedService<ImapServerHostedService>();

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(builder.Configuration, "relate-mail-imap");

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
                exception = e.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.Run();
