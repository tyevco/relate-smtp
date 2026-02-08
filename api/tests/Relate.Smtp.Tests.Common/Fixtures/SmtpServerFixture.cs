using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relate.Smtp.SmtpHost;
using Relate.Smtp.Infrastructure;
using Xunit;

namespace Relate.Smtp.Tests.Common.Fixtures;

/// <summary>
/// Provides an SMTP server for E2E protocol testing.
/// </summary>
public class SmtpServerFixture : IAsyncLifetime
{
    private IHost? _host;
    private readonly PostgresContainerFixture _postgres;
    private readonly bool _ownsPostgres;
    private int _port;

    public SmtpServerFixture()
    {
        _postgres = new PostgresContainerFixture();
        _ownsPostgres = true;
    }

    public SmtpServerFixture(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
        _ownsPostgres = false;
    }

    /// <summary>
    /// Gets the port the SMTP server is listening on.
    /// </summary>
    public int Port => _port;

    /// <summary>
    /// Gets the PostgreSQL fixture.
    /// </summary>
    public PostgresContainerFixture Postgres => _postgres;

    /// <summary>
    /// Gets the service provider for the SMTP server.
    /// </summary>
    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("Host not initialized");

    public async ValueTask InitializeAsync()
    {
        // Start PostgreSQL if we own it
        if (_ownsPostgres)
        {
            await _postgres.InitializeAsync();
        }

        // Find an available port
        _port = GetAvailablePort();

        // Build and start the host
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddInfrastructure(_postgres.ConnectionString);

                services.Configure<SmtpServerOptions>(options =>
                {
                    options.ServerName = "test-smtp";
                    options.Port = _port;
                    options.SecurePort = 0; // Disable SSL for tests
                    options.RequireAuthentication = true;
                });

                services.AddHostedService<SmtpServerHostedService>();

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                    builder.AddConsole();
                });
            })
            .Build();

        await _host.StartAsync();

        // Give the server a moment to start listening
        await Task.Delay(500);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        if (_ownsPostgres)
        {
            await _postgres.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    private static int GetAvailablePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

/// <summary>
/// Collection definition for sharing SmtpServerFixture across tests.
/// </summary>
[CollectionDefinition("SmtpServer")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - xUnit collection fixture convention
public class SmtpServerCollection : ICollectionFixture<SmtpServerFixture>
#pragma warning restore CA1711
{
}
