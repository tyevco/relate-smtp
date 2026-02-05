using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Telemetry;
using Relate.Smtp.Pop3Host;
using Relate.Smtp.Pop3Host.Handlers;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure services (database + repositories)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=relate_smtp;Username=postgres;Password=postgres";
builder.Services.AddInfrastructure(connectionString);

// Configure POP3 server options
builder.Services.Configure<Pop3ServerOptions>(
    builder.Configuration.GetSection("Pop3"));

// Register handlers (scoped per connection)
builder.Services.AddScoped<Pop3UserAuthenticator>();
builder.Services.AddScoped<Pop3CommandHandler>();
builder.Services.AddScoped<Pop3MessageManager>();

// Register hosted service
builder.Services.AddHostedService<Pop3ServerHostedService>();

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(builder.Configuration, "relate-mail-pop3");

var host = builder.Build();
host.Run();
