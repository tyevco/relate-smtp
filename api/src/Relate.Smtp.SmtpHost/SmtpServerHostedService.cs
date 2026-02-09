using System.Security.Cryptography.X509Certificates;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;
using Relate.Smtp.SmtpHost.Handlers;
using Relate.Smtp.Infrastructure.Services;
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
    public long MaxAttachmentSizeBytes { get; set; } = 25 * 1024 * 1024;  // 25 MB
    public long MaxMessageSizeBytes { get; set; } = 50 * 1024 * 1024;     // 50 MB

    /// <summary>
    /// MX endpoint configuration for accepting inbound mail from the internet.
    /// </summary>
    public MxEndpointOptions Mx { get; set; } = new();
}

/// <summary>
/// Configuration for the MX (Mail Exchange) endpoint that accepts unauthenticated
/// server-to-server mail delivery on port 25.
/// </summary>
public class MxEndpointOptions
{
    /// <summary>
    /// Whether the MX endpoint is enabled. Defaults to false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The port for the MX endpoint (unauthenticated inbound mail). Defaults to 25.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// The domains this server is authoritative for. Mail will only be accepted
    /// for recipients at these domains to prevent open relay.
    /// Example: ["example.com", "mail.example.com"]
    /// </summary>
    public string[] HostedDomains { get; set; } = [];

    /// <summary>
    /// Whether to validate that recipients exist in the database before accepting mail.
    /// When true, rejects mail to unknown users at hosted domains during the RCPT TO phase.
    /// Defaults to true.
    /// </summary>
    public bool ValidateRecipients { get; set; } = true;
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
            // Plain port with STARTTLS support (client submission)
            .Endpoint(endpoint =>
            {
                endpoint.Port(_options.Port, false);
                if (_options.RequireAuthentication)
                {
                    endpoint.AuthenticationRequired();
                    endpoint.AllowUnsecureAuthentication();
                }
            })
            // Secure port (implicit TLS, client submission)
            .Endpoint(endpoint =>
            {
                endpoint.Port(_options.SecurePort, true);
                if (_options.RequireAuthentication)
                {
                    endpoint.AuthenticationRequired();
                }
                if (!string.IsNullOrEmpty(_options.CertificatePath))
                {
                    endpoint.Certificate(LoadCertificate());
                }
            });

        // MX endpoint (port 25) - unauthenticated server-to-server delivery
        if (_options.Mx.Enabled)
        {
            if (_options.Mx.HostedDomains.Length == 0)
            {
                throw new InvalidOperationException(
                    "Smtp:Mx:HostedDomains must be configured when MX endpoint is enabled. " +
                    "This prevents the server from acting as an open relay.");
            }

            optionsBuilder.Endpoint(endpoint =>
            {
                endpoint.Port(_options.Mx.Port, false);
                // No AuthenticationRequired() — MX accepts unauthenticated mail
                if (!string.IsNullOrEmpty(_options.CertificatePath))
                {
                    endpoint.Certificate(LoadCertificate());
                }
            });

            _logger.LogInformation(
                "MX endpoint enabled on port {MxPort} for domains: {Domains}",
                _options.Mx.Port,
                string.Join(", ", _options.Mx.HostedDomains));
        }

        var smtpServerOptions = optionsBuilder.Build();

        var serviceProvider = new SmtpServer.ComponentModel.ServiceProvider();
        serviceProvider.Add(CreateMessageStore());
        serviceProvider.Add(CreateUserAuthenticator());
        serviceProvider.Add((IMailboxFilterFactory)CreateMailboxFilter());

        _smtpServer = new SmtpServer.SmtpServer(smtpServerOptions, serviceProvider);

        // Subscribe to session events for connection metrics (if metrics are available)
        _smtpServer.SessionCreated += (_, _) =>
        {
#pragma warning disable CA1031 // Metrics are best-effort, failures should not crash the server
            try { ProtocolMetrics.SmtpActiveConnections.Add(1); } catch { /* ignore */ }
#pragma warning restore CA1031
        };
        _smtpServer.SessionCompleted += (_, _) =>
        {
#pragma warning disable CA1031 // Metrics are best-effort, failures should not crash the server
            try { ProtocolMetrics.SmtpActiveConnections.Add(-1); } catch { /* ignore */ }
#pragma warning restore CA1031
        };

        _logger.LogInformation("Starting SMTP server on port {Port} (plain) and {SecurePort} (TLS)",
            _options.Port, _options.SecurePort);

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

    private CustomMessageStore CreateMessageStore()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<CustomMessageStore>>();
        return new CustomMessageStore(_serviceProvider, logger, _options);
    }

    private CustomUserAuthenticator CreateUserAuthenticator()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<CustomUserAuthenticator>>();
        var backgroundTaskQueue = _serviceProvider.GetRequiredService<IBackgroundTaskQueue>();
        var rateLimiter = _serviceProvider.GetRequiredService<IAuthenticationRateLimiter>();
        return new CustomUserAuthenticator(_serviceProvider, logger, backgroundTaskQueue, rateLimiter);
    }

    private MxMailboxFilter CreateMailboxFilter()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MxMailboxFilter>>();
        return new MxMailboxFilter(_serviceProvider, logger, _options);
    }

    private X509Certificate2 LoadCertificate()
    {
        if (string.IsNullOrEmpty(_options.CertificatePath))
        {
            throw new InvalidOperationException("Certificate path is required for secure port");
        }

        return string.IsNullOrEmpty(_options.CertificatePassword)
            ? X509CertificateLoader.LoadCertificateFromFile(_options.CertificatePath)
            : X509CertificateLoader.LoadPkcs12FromFile(_options.CertificatePath, _options.CertificatePassword);
    }
}
