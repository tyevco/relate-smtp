using Microsoft.AspNetCore.Mvc;
using Relate.Smtp.Api.Models;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreferencesController : ControllerBase
{
    private readonly IUserPreferenceRepository _preferenceRepository;

    public PreferencesController(IUserPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    [HttpGet]
    public async Task<ActionResult<UserPreferenceDto>> GetPreferences(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        var preference = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (preference == null)
        {
            // Return default preferences if none exist yet
            return Ok(new UserPreferenceDto
            {
                Id = Guid.Empty,
                UserId = userId,
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
        var userId = GetUserId();

        var existing = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        var preference = existing ?? new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
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
