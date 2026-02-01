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

        // Add enabled protocol features
        if (bool.Parse(_configuration["Smtp:Enabled"] ?? "true"))
            features.Add("smtp");
        if (bool.Parse(_configuration["Pop3:Enabled"] ?? "true"))
            features.Add("pop3");
        if (bool.Parse(_configuration["Imap:Enabled"] ?? "true"))
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
