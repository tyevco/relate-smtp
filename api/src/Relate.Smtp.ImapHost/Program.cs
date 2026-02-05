using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.ImapHost;
using Relate.Smtp.ImapHost.Handlers;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure services (database + repositories)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=relate_smtp;Username=postgres;Password=postgres";
builder.Services.AddInfrastructure(connectionString);

// Configure IMAP server options
builder.Services.Configure<ImapServerOptions>(
    builder.Configuration.GetSection("Imap"));

// Register handlers (scoped per connection)
builder.Services.AddScoped<ImapUserAuthenticator>();
builder.Services.AddScoped<ImapCommandHandler>();
builder.Services.AddScoped<ImapMessageManager>();

// Register hosted service
builder.Services.AddHostedService<ImapServerHostedService>();

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(builder.Configuration, "relate-mail-imap");

var host = builder.Build();
host.Run();
