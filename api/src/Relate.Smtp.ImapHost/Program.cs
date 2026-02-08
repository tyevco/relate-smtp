using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.ImapHost;
using Relate.Smtp.ImapHost.Handlers;

var builder = Host.CreateApplicationBuilder(args);

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
