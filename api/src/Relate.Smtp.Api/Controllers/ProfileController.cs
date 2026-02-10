using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly UserProvisioningService _userProvisioningService;

    public ProfileController(
        IUserRepository userRepository,
        IEmailRepository emailRepository,
        UserProvisioningService userProvisioningService)
    {
        _userRepository = userRepository;
        _emailRepository = emailRepository;
        _userProvisioningService = userProvisioningService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        return Ok(user.ToDto());
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Ok(user.ToDto());
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<EmailAddressDto>> AddEmailAddress(
        [FromBody] AddEmailAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest(new { error = "Email address is required" });
        }

        string normalizedAddress;
        try
        {
            var mailAddress = new MailAddress(request.Address);
            // Normalize to the parsed address
            normalizedAddress = mailAddress.Address;
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid email address format" });
        }

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        // Check if address already exists
        if (user.Email.Equals(normalizedAddress, StringComparison.OrdinalIgnoreCase) ||
            user.AdditionalAddresses.Any(a => a.Address.Equals(normalizedAddress, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = "Email address already registered" });
        }

        var address = new UserEmailAddress
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Address = normalizedAddress,
            IsVerified = false,
            VerificationToken = GenerateVerificationCode(),
            VerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            AddedAt = DateTimeOffset.UtcNow
        };

        await _userRepository.AddEmailAddressAsync(address, cancellationToken);

        // Link existing emails to this user
        await _emailRepository.LinkEmailsToUserAsync(user.Id, new[] { normalizedAddress }, cancellationToken);

        return Ok(new EmailAddressDto(address.Id, address.Address, address.IsVerified, address.AddedAt));
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> RemoveEmailAddress(Guid addressId, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var address = user.AdditionalAddresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
        {
            return NotFound();
        }

        await _userRepository.RemoveEmailAddressAsync(addressId, cancellationToken);

        return NoContent();
    }

    // Intentionally disabled until outbound email sending is wired up.
    // Re-enable once the platform can deliver verification codes via email.
    [HttpPost("addresses/{addressId:guid}/send-verification")]
    public Task<IActionResult> SendVerification(Guid addressId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented,
            new { error = "Email verification is not yet available" }));
    }

    [HttpPost("addresses/{addressId:guid}/verify")]
    public Task<ActionResult<EmailAddressDto>> VerifyEmailAddress(
        Guid addressId,
        [FromBody] VerifyEmailAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ActionResult<EmailAddressDto>>(StatusCode(StatusCodes.Status501NotImplemented,
            new { error = "Email verification is not yet available" }));
    }

    private static string GenerateVerificationCode()
    {
        // 8-char alphanumeric code (ambiguous chars removed) for brute-force resistance
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(8, chars, static (span, c) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = c[RandomNumberGenerator.GetInt32(c.Length)];
        });
    }
}
