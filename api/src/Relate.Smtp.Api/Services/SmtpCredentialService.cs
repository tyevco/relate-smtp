using System.Security.Cryptography;

namespace Relate.Smtp.Api.Services;

public class SmtpCredentialService
{
    private const int ApiKeyBytes = 32;
    private const int BCryptWorkFactor = 11;
    public const int KeyPrefixLength = 12;

    public string GenerateApiKey()
    {
        var bytes = new byte[ApiKeyBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Extracts the prefix from an API key for efficient database lookup.
    /// </summary>
    public string ExtractKeyPrefix(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < KeyPrefixLength)
        {
            return apiKey;
        }
        return apiKey[..KeyPrefixLength];
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCryptWorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
