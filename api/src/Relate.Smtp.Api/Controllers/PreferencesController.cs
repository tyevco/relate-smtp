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
public class PreferencesController : ControllerBase
{
    private readonly IUserPreferenceRepository _preferenceRepository;
    private readonly UserProvisioningService _userProvisioningService;

    public PreferencesController(
        IUserPreferenceRepository preferenceRepository,
        UserProvisioningService userProvisioningService)
    {
        _preferenceRepository = preferenceRepository;
        _userProvisioningService = userProvisioningService;
    }

    [HttpGet]
    public async Task<ActionResult<UserPreferenceDto>> GetPreferences(CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var preference = await _preferenceRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (preference == null)
        {
            // Return default preferences if none exist yet
            return Ok(new UserPreferenceDto
            {
                Id = Guid.Empty,
                UserId = user.Id,
                Theme = "system",
                DisplayDensity = "comfortable",
                EmailsPerPage = 20,
                DefaultSort = "receivedAt-desc",
                ShowPreview = true,
                GroupByDate = false,
                DesktopNotifications = false,
                EmailDigest = false,
                DigestFrequency = "daily",
                DigestTime = new TimeOnly(9, 0),
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        return Ok(MapToDto(preference));
    }

    [HttpPut]
    public async Task<ActionResult<UserPreferenceDto>> UpdatePreferences(
        [FromBody] UpdateUserPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var existing = await _preferenceRepository.GetByUserIdAsync(user.Id, cancellationToken);

        var preference = existing ?? new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

        // Update only provided fields
        if (request.Theme != null) preference.Theme = request.Theme;
        if (request.DisplayDensity != null) preference.DisplayDensity = request.DisplayDensity;
        if (request.EmailsPerPage.HasValue) preference.EmailsPerPage = request.EmailsPerPage.Value;
        if (request.DefaultSort != null) preference.DefaultSort = request.DefaultSort;
        if (request.ShowPreview.HasValue) preference.ShowPreview = request.ShowPreview.Value;
        if (request.GroupByDate.HasValue) preference.GroupByDate = request.GroupByDate.Value;
        if (request.DesktopNotifications.HasValue) preference.DesktopNotifications = request.DesktopNotifications.Value;
        if (request.EmailDigest.HasValue) preference.EmailDigest = request.EmailDigest.Value;
        if (request.DigestFrequency != null) preference.DigestFrequency = request.DigestFrequency;
        if (request.DigestTime.HasValue) preference.DigestTime = request.DigestTime.Value;

        preference.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await _preferenceRepository.UpsertAsync(preference, cancellationToken);

        return Ok(MapToDto(updated));
    }

    private static UserPreferenceDto MapToDto(UserPreference preference)
    {
        return new UserPreferenceDto
        {
            Id = preference.Id,
            UserId = preference.UserId,
            Theme = preference.Theme,
            DisplayDensity = preference.DisplayDensity,
            EmailsPerPage = preference.EmailsPerPage,
            DefaultSort = preference.DefaultSort,
            ShowPreview = preference.ShowPreview,
            GroupByDate = preference.GroupByDate,
            DesktopNotifications = preference.DesktopNotifications,
            EmailDigest = preference.EmailDigest,
            DigestFrequency = preference.DigestFrequency,
            DigestTime = preference.DigestTime,
            UpdatedAt = preference.UpdatedAt
        };
    }
}
