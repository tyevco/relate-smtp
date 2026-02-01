using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relate.Smtp.ImapHost;
using Relate.Smtp.ImapHost.Handlers;
using Relate.Smtp.Infrastructure;
using Xunit;

namespace Relate.Smtp.Tests.Common.Fixtures;

/// <summary>
/// Provides an IMAP server for E2E protocol testing.
/// </summary>
public class ImapServerFixture : IAsyncLifetime
{
    private IHost? _host;
    private readonly PostgresContainerFixture _postgres;
    private readonly bool _ownsPostgres;
    private int _port;

    public ImapServerFixture()
    {
        _postgres = new PostgresContainerFixture();
        _ownsPostgres = true;
    }

    public ImapServerFixture(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
        _ownsPostgres = false;
    }

    /// <summary>
    /// Gets the port the IMAP server is listening on.
    /// </summary>
    public int Port => _port;

    /// <summary>
    /// Gets the PostgreSQL fixture.
    /// </summary>
    public PostgresContainerFixture Postgres => _postgres;

    /// <summary>
    /// Gets the service provider for the IMAP server.
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

                services.Configure<ImapServerOptions>(options =>
                {
                    options.ServerName = "test-imap";
                    options.Port = _port;
                    options.SecurePort = 0; // Disable SSL for tests
                    options.RequireAuthentication = true;
                    options.SessionTimeout = TimeSpan.FromMinutes(5);
                });

                services.AddSingleton<ImapUserAuthenticator>();
                services.AddScoped<ImapCommandHandler>();
                services.AddScoped<ImapMessageManager>();
                services.AddHostedService<ImapServerHostedService>();

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
/// Collection definition for sharing ImapServerFixture across tests.
/// </summary>
[CollectionDefinition("ImapServer")]
public class ImapServerCollection : ICollectionFixture<ImapServerFixture>
{
}
