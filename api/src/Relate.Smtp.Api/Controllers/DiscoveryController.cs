using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/discovery")]
public class DiscoveryController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DiscoveryController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns server capabilities for mobile app setup.
    /// This endpoint is public and does not require authentication.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ServerDiscoveryDto> GetDiscovery()
    {
        var oidcAuthority = _configuration["Oidc:Authority"];
        var oidcEnabled = !string.IsNullOrEmpty(oidcAuthority);

        var features = new List<string>();

        // Add enabled protocol features (defaults to true if not configured or invalid)
        if (!bool.TryParse(_configuration["Smtp:Enabled"], out var smtpEnabled) || smtpEnabled)
            features.Add("smtp");
        if (!bool.TryParse(_configuration["Pop3:Enabled"], out var pop3Enabled) || pop3Enabled)
            features.Add("pop3");
        if (!bool.TryParse(_configuration["Imap:Enabled"], out var imapEnabled) || imapEnabled)
            features.Add("imap");

        // Add other features
        features.Add("api-keys");
        features.Add("labels");
        features.Add("filters");
        features.Add("preferences");

        if (oidcEnabled)
            features.Add("oidc");

        return Ok(new ServerDiscoveryDto(
            Version: "1.0.0",
            ApiVersion: "v1",
            OidcEnabled: oidcEnabled,
            Features: features
        ));
    }
}

public record ServerDiscoveryDto(
    string Version,
    string ApiVersion,
    bool OidcEnabled,
    IReadOnlyList<string> Features
);
