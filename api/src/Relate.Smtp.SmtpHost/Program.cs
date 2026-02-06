using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.SmtpHost;
using Relate.Smtp.SmtpHost.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=relate_smtp;Username=postgres;Password=postgres";

builder.Services.AddInfrastructure(connectionString);

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

// Add SMTP server hosted service
builder.Services.AddHostedService<SmtpServerHostedService>();

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(builder.Configuration, "relate-mail-smtp");

var host = builder.Build();
host.Run();
