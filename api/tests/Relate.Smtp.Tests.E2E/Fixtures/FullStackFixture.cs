using Relate.Smtp.Tests.Common.Fixtures;

namespace Relate.Smtp.Tests.E2E.Fixtures;

/// <summary>
/// Provides a complete test environment with API, SMTP, POP3, and IMAP servers
/// all sharing the same PostgreSQL database.
/// </summary>
public class FullStackFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private SmtpServerFixture? _smtp;
    private Pop3ServerFixture? _pop3;
    private ImapServerFixture? _imap;

    public FullStackFixture()
    {
        _postgres = new PostgresContainerFixture();
    }

    public PostgresContainerFixture Postgres => _postgres;
    public SmtpServerFixture Smtp => _smtp ?? throw new InvalidOperationException("SMTP not initialized");
    public Pop3ServerFixture Pop3 => _pop3 ?? throw new InvalidOperationException("POP3 not initialized");
    public ImapServerFixture Imap => _imap ?? throw new InvalidOperationException("IMAP not initialized");

    public async ValueTask InitializeAsync()
    {
        // Start shared PostgreSQL
        await _postgres.InitializeAsync();

        // Start protocol servers sharing the same database
        _smtp = new SmtpServerFixture(_postgres);
        _pop3 = new Pop3ServerFixture(_postgres);
        _imap = new ImapServerFixture(_postgres);

        await _smtp.InitializeAsync();
        await _pop3.InitializeAsync();
        await _imap.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_imap != null) await _imap.DisposeAsync();
        if (_pop3 != null) await _pop3.DisposeAsync();
        if (_smtp != null) await _smtp.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Resets the database between tests.
    /// </summary>
    public Task ResetAsync() => _postgres.ResetDatabaseAsync();
}

[CollectionDefinition("FullStack")]
public class FullStackCollection : ICollectionFixture<FullStackFixture>
{
}
