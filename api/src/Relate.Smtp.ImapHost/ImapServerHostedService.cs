using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relate.Smtp.Infrastructure.Telemetry;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Relate.Smtp.ImapHost;

public class ImapServerHostedService : BackgroundService
{
    private readonly ILogger<ImapServerHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ImapServerOptions> _options;
    private readonly ConcurrentBag<Task> _activeTasks = new();
    private TcpListener? _tcpListener;
    private TcpListener? _sslListener;

    public ImapServerHostedService(
        ILogger<ImapServerHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<ImapServerOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        // Start plain IMAP listener (port 143)
        _tcpListener = new TcpListener(IPAddress.Any, options.Port);
        _tcpListener.Start();
        _logger.LogInformation("IMAP server listening on port {Port}", options.Port);

        // Start IMAPS listener (port 993) if certificate configured
        if (options.SecurePort > 0 && !string.IsNullOrEmpty(options.CertificatePath))
        {
            _sslListener = new TcpListener(IPAddress.Any, options.SecurePort);
            _sslListener.Start();
            _logger.LogInformation("IMAPS server listening on port {Port}", options.SecurePort);
        }
        else if (options.SecurePort > 0)
        {
            _logger.LogWarning("IMAPS port configured but no certificate path provided. SSL/TLS disabled.");
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
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(client, useSsl, ct);
                    }
#pragma warning disable CA1031 // Do not catch general exception types - Client handler must not crash server
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _logger.LogError(ex, "Unhandled exception in client handler for {Endpoint}", endpoint);
                    }
                }, ct);
                _activeTasks.Add(task);
            }
            catch (OperationCanceledException)
            {
                break;
            }
#pragma warning disable CA1031 // Do not catch general exception types - Accept loop must continue on errors
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Error accepting connection");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, bool useSsl, CancellationToken ct)
    {
        ProtocolMetrics.ImapActiveSessions.Add(1);

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<Handlers.ImapCommandHandler>();

        // Extract client IP for rate limiting
        var clientIp = client.Client.RemoteEndPoint?.ToString()?.Split(':')[0] ?? "unknown";

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
#pragma warning disable CA5398 // Avoid hardcoding SslProtocols - TLS 1.2+ is required for email security
                await sslStream.AuthenticateAsServerAsync(
                    cert,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: _options.Value.CheckCertificateRevocation);
#pragma warning restore CA5398
                stream = sslStream;
                _logger.LogDebug("SSL/TLS handshake completed");
            }

            await handler.HandleSessionAsync(stream, clientIp, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Client handler cancelled");
        }
        catch (IOException ex) when (ex.Message.Contains("Broken pipe", StringComparison.Ordinal) || ex.InnerException?.Message.Contains("Broken pipe", StringComparison.Ordinal) == true)
        {
            _logger.LogDebug("Client disconnected before greeting (broken pipe)");
        }
#pragma warning disable CA1031 // Do not catch general exception types - Protocol handler must gracefully handle all errors
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Client handler error");
        }
        finally
        {
            ProtocolMetrics.ImapActiveSessions.Add(-1);

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
        _logger.LogInformation("Stopping IMAP server...");
        _tcpListener?.Stop();
        _sslListener?.Stop();

        var pending = _activeTasks.Where(t => !t.IsCompleted).ToArray();
        if (pending.Length > 0)
        {
            _logger.LogInformation("Waiting for {Count} active client connections to complete...", pending.Length);
            await Task.WhenAny(Task.WhenAll(pending), Task.Delay(TimeSpan.FromSeconds(30), cancellationToken));
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("IMAP server stopped");
    }
}
