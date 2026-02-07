using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relate.Smtp.Infrastructure.Telemetry;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Relate.Smtp.Pop3Host;

public class Pop3ServerHostedService : BackgroundService
{
    private readonly ILogger<Pop3ServerHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<Pop3ServerOptions> _options;
    private TcpListener? _tcpListener;
    private TcpListener? _sslListener;

    public Pop3ServerHostedService(
        ILogger<Pop3ServerHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<Pop3ServerOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        // Start plain POP3 listener (port 110)
        _tcpListener = new TcpListener(IPAddress.Any, options.Port);
        _tcpListener.Start();
        _logger.LogInformation("POP3 server listening on port {Port}", options.Port);

        // Start POP3S listener (port 995) if certificate configured
        if (options.SecurePort > 0 && !string.IsNullOrEmpty(options.CertificatePath))
        {
            _sslListener = new TcpListener(IPAddress.Any, options.SecurePort);
            _sslListener.Start();
            _logger.LogInformation("POP3S server listening on port {Port}", options.SecurePort);
        }
        else if (options.SecurePort > 0)
        {
            _logger.LogWarning("POP3S port configured but no certificate path provided. SSL/TLS disabled.");
        }

        var plainTask = AcceptConnectionsAsync(_tcpListener, false, stoppingToken);
        var sslTask = _sslListener != null
            ? AcceptConnectionsAsync(_sslListener, true, stoppingToken)
            : Task.CompletedTask;

        await Task.WhenAll(plainTask, sslTask);
    }

    private async Task AcceptConnectionsAsync(
        TcpListener listener,
        bool useSsl,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                _logger.LogDebug("Connection accepted from {Endpoint} (SSL: {UseSsl})", endpoint, useSsl);

                // Handle client in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(client, useSsl, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception in client handler for {Endpoint}", endpoint);
                    }
                }, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting connection");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, bool useSsl, CancellationToken ct)
    {
        ProtocolMetrics.Pop3ActiveSessions.Add(1);

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<Handlers.Pop3CommandHandler>();

        SslStream? sslStream = null;
        X509Certificate2? cert = null;

        try
        {
            Stream stream = client.GetStream();

            if (useSsl)
            {
                sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                cert = LoadCertificate(_options.Value.CertificatePath!,
                                       _options.Value.CertificatePassword);
                await sslStream.AuthenticateAsServerAsync(
                    cert,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false);
                stream = sslStream;
                _logger.LogDebug("SSL/TLS handshake completed");
            }

            await handler.HandleSessionAsync(stream, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Client handler cancelled");
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe") || ex.InnerException?.Message.Contains("Broken pipe") == true)
        {
            _logger.LogDebug("Client disconnected before greeting (broken pipe)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client handler error");
        }
        finally
        {
            ProtocolMetrics.Pop3ActiveSessions.Add(-1);

            // Dispose SSL stream and certificate properly
            if (sslStream != null)
            {
                await sslStream.DisposeAsync();
            }
            cert?.Dispose();
            client.Close();
        }
    }

    private X509Certificate2 LoadCertificate(string path, string? password)
    {
        try
        {
            return string.IsNullOrEmpty(password)
                ? X509CertificateLoader.LoadCertificateFromFile(path)
                : X509CertificateLoader.LoadPkcs12FromFile(path, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate from {Path}", path);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping POP3 server...");
        _tcpListener?.Stop();
        _sslListener?.Stop();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("POP3 server stopped");
    }
}
