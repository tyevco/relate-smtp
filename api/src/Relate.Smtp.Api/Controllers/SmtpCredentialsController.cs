using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/smtp-credentials")]
[Authorize]
public class SmtpCredentialsController : ControllerBase
{
    private readonly ISmtpApiKeyRepository _apiKeyRepository;
    private readonly SmtpCredentialService _credentialService;
    private readonly UserProvisioningService _userProvisioningService;
    private readonly IConfiguration _configuration;

    public SmtpCredentialsController(
        ISmtpApiKeyRepository apiKeyRepository,
        SmtpCredentialService credentialService,
        UserProvisioningService userProvisioningService,
        IConfiguration configuration)
    {
        _apiKeyRepository = apiKeyRepository;
        _credentialService = credentialService;
        _userProvisioningService = userProvisioningService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<SmtpCredentialsDto>> GetCredentials(CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var keys = await _apiKeyRepository.GetActiveKeysForUserAsync(user.Id, cancellationToken);

        var connectionInfo = new SmtpConnectionInfoDto(
            SmtpServer: _configuration["Smtp:ServerName"] ?? "localhost",
            SmtpPort: int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SmtpSecurePort: int.Parse(_configuration["Smtp:SecurePort"] ?? "465"),
            SmtpEnabled: bool.Parse(_configuration["Smtp:Enabled"] ?? "true"),
            Pop3Server: _configuration["Pop3:ServerName"] ?? "localhost",
            Pop3Port: int.Parse(_configuration["Pop3:Port"] ?? "110"),
            Pop3SecurePort: int.Parse(_configuration["Pop3:SecurePort"] ?? "995"),
            Pop3Enabled: bool.Parse(_configuration["Pop3:Enabled"] ?? "true"),
            ImapServer: _configuration["Imap:ServerName"] ?? "localhost",
            ImapPort: int.Parse(_configuration["Imap:Port"] ?? "143"),
            ImapSecurePort: int.Parse(_configuration["Imap:SecurePort"] ?? "993"),
            ImapEnabled: bool.Parse(_configuration["Imap:Enabled"] ?? "true"),
            Username: user.Email,
            ActiveKeyCount: keys.Count
        );

        var keyDtos = keys.Select(k => new SmtpApiKeyDto(
            k.Id,
            k.Name,
            k.CreatedAt,
            k.LastUsedAt,
            k.RevokedAt == null,
            _apiKeyRepository.ParseScopes(k.Scopes)
        )).ToList();

        return Ok(new SmtpCredentialsDto(connectionInfo, keyDtos));
    }

    [HttpPost]
    public async Task<ActionResult<CreatedSmtpApiKeyDto>> CreateApiKey(
        [FromBody] CreateSmtpApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        if (request.Name.Length > 100)
        {
            return BadRequest(new { error = "Name must be 100 characters or less" });
        }

        // Validate and normalize scopes
        var scopes = request.Scopes ?? ApiKeyScopes.AllScopes.ToList();
        if (scopes.Count == 0)
        {
            scopes = ApiKeyScopes.AllScopes.ToList(); // Default to all
        }

        foreach (var scope in scopes)
        {
            if (!ApiKeyScopes.IsValidScope(scope))
            {
                return BadRequest(new { error = $"Invalid scope: {scope}. Valid scopes are: {string.Join(", ", ApiKeyScopes.AllScopes)}" });
            }
        }

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var apiKey = _credentialService.GenerateApiKey();
        var keyHash = _credentialService.HashPassword(apiKey);

        var smtpApiKey = new SmtpApiKey
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            KeyHash = keyHash,
            Scopes = JsonSerializer.Serialize(scopes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _apiKeyRepository.CreateAsync(smtpApiKey, cancellationToken);

        return Ok(new CreatedSmtpApiKeyDto(
            smtpApiKey.Id,
            smtpApiKey.Name,
            apiKey,
            scopes,
            smtpApiKey.CreatedAt
        ));
    }

    [HttpDelete("{keyId:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid keyId, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var keys = await _apiKeyRepository.GetActiveKeysForUserAsync(user.Id, cancellationToken);

        if (!keys.Any(k => k.Id == keyId))
        {
            return NotFound();
        }

        await _apiKeyRepository.RevokeAsync(keyId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Create an API key from a mobile device during OIDC bootstrap.
    /// This endpoint is called after successful OIDC authentication to generate
    /// a persistent API key for the mobile app.
    /// </summary>
    [HttpPost("mobile")]
    public async Task<ActionResult<CreatedSmtpApiKeyDto>> CreateMobileApiKey(
        [FromBody] CreateMobileApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceName))
        {
            return BadRequest(new { error = "DeviceName is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Platform))
        {
            return BadRequest(new { error = "Platform is required" });
        }

        var validPlatforms = new[] { "ios", "android", "windows", "macos", "web" };
        var platform = request.Platform.ToLowerInvariant();
        if (!validPlatforms.Contains(platform))
        {
            return BadRequest(new { error = $"Invalid platform: {request.Platform}. Valid platforms are: {string.Join(", ", validPlatforms)}" });
        }

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        // Mobile apps get api:read and api:write scopes by default
        var scopes = new List<string> { ApiKeyScopes.ApiRead, ApiKeyScopes.ApiWrite };

        // Generate a descriptive name for the key
        var keyName = $"Mobile App - {request.DeviceName} ({platform})";
        if (keyName.Length > 100)
        {
            keyName = keyName.Substring(0, 100);
        }

        var apiKey = _credentialService.GenerateApiKey();
        var keyHash = _credentialService.HashPassword(apiKey);

        var smtpApiKey = new SmtpApiKey
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = keyName,
            KeyHash = keyHash,
            Scopes = JsonSerializer.Serialize(scopes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _apiKeyRepository.CreateAsync(smtpApiKey, cancellationToken);

        return Ok(new CreatedSmtpApiKeyDto(
            smtpApiKey.Id,
            smtpApiKey.Name,
            apiKey,
            scopes,
            smtpApiKey.CreatedAt
        ));
    }
}
