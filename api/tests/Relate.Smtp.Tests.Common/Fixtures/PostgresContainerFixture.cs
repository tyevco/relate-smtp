using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Relate.Smtp.Tests.Common.Fixtures;

/// <summary>
/// Provides a PostgreSQL container for integration tests using Testcontainers.
/// This fixture manages the lifecycle of a PostgreSQL container and provides
/// factory methods for creating database contexts.
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Gets the connection string for the PostgreSQL container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container not initialized");

    /// <summary>
    /// Initializes the PostgreSQL container and applies EF Core migrations.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("relate_smtp_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Apply migrations
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Stops and disposes the PostgreSQL container.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new DbContext instance connected to the test database.
    /// </summary>
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Creates DbContextOptions for the test database.
    /// </summary>
    public DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    /// <summary>
    /// Resets the database by truncating all tables.
    /// Use this between tests for isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // Disable foreign key checks, truncate all tables, re-enable
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
                    EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' CASCADE';
                END LOOP;
            END $$;
        ");
    }

    /// <summary>
    /// Clears specific tables from the database.
    /// </summary>
    public async Task ClearTablesAsync(params string[] tableNames)
    {
        await using var context = CreateDbContext();

        foreach (var tableName in tableNames)
        {
            await context.Database.ExecuteSqlRawAsync(
                $"TRUNCATE TABLE \"{tableName}\" CASCADE");
        }
    }
}

/// <summary>
/// Collection definition for sharing PostgresContainerFixture across tests.
/// </summary>
[CollectionDefinition("PostgresDatabase")]
public class PostgresDatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
}
