using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;
using Relate.Smtp.SmtpHost.Handlers;
using Relate.Smtp.Infrastructure.Telemetry;
using Microsoft.Extensions.Options;

namespace Relate.Smtp.SmtpHost;

public class SmtpServerOptions
{
    public string ServerName { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public int SecurePort { get; set; } = 465;
    public bool RequireAuthentication { get; set; } = true;
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
}

public class SmtpServerHostedService : BackgroundService
{
    private readonly ILogger<SmtpServerHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SmtpServerOptions _options;
    private SmtpServer.SmtpServer? _smtpServer;

    public SmtpServerHostedService(
        ILogger<SmtpServerHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<SmtpServerOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var optionsBuilder = new SmtpServerOptionsBuilder()
            .ServerName(_options.ServerName)
            .Endpoint(endpoint =>
            {
                endpoint.Port(_options.Port, false);
                if (_options.RequireAuthentication)
                {
                    endpoint.AuthenticationRequired();
                    endpoint.AllowUnsecureAuthentication();
                }
            });

        var smtpServerOptions = optionsBuilder.Build();

        var serviceProvider = new SmtpServer.ComponentModel.ServiceProvider();
        serviceProvider.Add(CreateMessageStore());
        serviceProvider.Add(CreateUserAuthenticator());

        _smtpServer = new SmtpServer.SmtpServer(smtpServerOptions, serviceProvider);

        // Subscribe to session events for connection metrics (if metrics are available)
        _smtpServer.SessionCreated += (_, _) =>
        {
            try { ProtocolMetrics.SmtpActiveConnections.Add(1); } catch { /* ignore */ }
        };
        _smtpServer.SessionCompleted += (_, _) =>
        {
            try { ProtocolMetrics.SmtpActiveConnections.Add(-1); } catch { /* ignore */ }
        };

        _logger.LogInformation("Starting SMTP server on port {Port}", _options.Port);

        try
        {
            await _smtpServer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SMTP server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP server error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SMTP server...");
        _smtpServer?.Shutdown();
        await base.StopAsync(cancellationToken);
    }

    private IMessageStore CreateMessageStore()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<CustomMessageStore>>();
        return new CustomMessageStore(_serviceProvider, logger);
    }

    private IUserAuthenticator CreateUserAuthenticator()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<CustomUserAuthenticator>>();
        return new CustomUserAuthenticator(_serviceProvider, logger);
    }

}
