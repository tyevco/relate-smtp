namespace Relate.Smtp.Api.Models;

public static class ApiKeyScopes
{
    public const string Smtp = "smtp";
    public const string Pop3 = "pop3";
    public const string Imap = "imap";
    public const string ApiRead = "api:read";
    public const string ApiWrite = "api:write";
    public const string App = "app";

    public static readonly string[] AllScopes = { Smtp, Pop3, Imap, ApiRead, ApiWrite, App };

    public static bool IsValidScope(string scope)
    {
        return AllScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}

public record SmtpApiKeyDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    IReadOnlyList<string> Scopes
);

public record CreateSmtpApiKeyRequest
{
    public required string Name { get; init; }

    /// <summary>
    /// Permission scopes for this key. Valid values: smtp, pop3, api:read, api:write
    /// If empty or null, defaults to all scopes for backward compatibility.
    /// </summary>
    public List<string>? Scopes { get; init; }
}

public record CreatedSmtpApiKeyDto(
    Guid Id,
    string Name,
    string ApiKey,
    IReadOnlyList<string> Scopes,
    DateTimeOffset CreatedAt
);

public record SmtpConnectionInfoDto(
    string SmtpServer,
    int SmtpPort,
    int SmtpSecurePort,
    bool SmtpEnabled,
    string Pop3Server,
    int Pop3Port,
    int Pop3SecurePort,
    bool Pop3Enabled,
    string ImapServer,
    int ImapPort,
    int ImapSecurePort,
    bool ImapEnabled,
    string Username,
    int ActiveKeyCount
);

public record SmtpCredentialsDto(
    SmtpConnectionInfoDto ConnectionInfo,
    IReadOnlyList<SmtpApiKeyDto> Keys
);

/// <summary>
/// Request to create an API key from a mobile device during OIDC bootstrap.
/// </summary>
public record CreateMobileApiKeyRequest
{
    public required string DeviceName { get; init; }
    public required string Platform { get; init; } // ios, android, windows, macos, web
}
