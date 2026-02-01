using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relate.Smtp.Infrastructure.Data;
using Relate.Smtp.Tests.Common.Helpers;
using Xunit;

namespace Relate.Smtp.Tests.Common.Fixtures;

/// <summary>
/// Provides a configured WebApplicationFactory for API integration testing.
/// Uses Testcontainers for PostgreSQL and test authentication.
/// </summary>
public class ApiServerFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private readonly PostgresContainerFixture _postgres;

    public ApiServerFixture()
    {
        _postgres = new PostgresContainerFixture();
    }

    /// <summary>
    /// Gets the WebApplicationFactory instance.
    /// </summary>
    public WebApplicationFactory<Program> Factory => _factory
        ?? throw new InvalidOperationException("Factory not initialized");

    /// <summary>
    /// Gets the PostgreSQL fixture for direct database access.
    /// </summary>
    public PostgresContainerFixture Postgres => _postgres;

    public async ValueTask InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgres.InitializeAsync();

        // Create web application factory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<AppDbContext>();

                    // Add test database
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_postgres.ConnectionString);
                    });

                    // Replace authentication with test handler
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName, _ => { });
                });

                builder.UseEnvironment("Testing");
            });
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Creates an HttpClient configured with test authentication.
    /// </summary>
    public HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>
    /// Creates an HttpClient authenticated as the specified user.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        Guid userId,
        string email,
        string? name = null)
    {
        var client = Factory.CreateClient();
        return client.WithTestUser(userId, email, name);
    }

    /// <summary>
    /// Creates a service scope for accessing DI services.
    /// </summary>
    public IServiceScope CreateScope() => Factory.Services.CreateScope();

    /// <summary>
    /// Gets a service from the factory's service provider.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull
    {
        using var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Resets the database for test isolation.
    /// </summary>
    public Task ResetDatabaseAsync() => _postgres.ResetDatabaseAsync();
}

/// <summary>
/// Collection definition for sharing ApiServerFixture across tests.
/// </summary>
[CollectionDefinition("ApiServer")]
public class ApiServerCollection : ICollectionFixture<ApiServerFixture>
{
}
