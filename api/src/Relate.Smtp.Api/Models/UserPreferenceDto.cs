namespace Relate.Smtp.Api.Models;

public class UserPreferenceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Display preferences
    public string Theme { get; set; } = "system";
    public string DisplayDensity { get; set; } = "comfortable";
    public int EmailsPerPage { get; set; } = 20;
    public string DefaultSort { get; set; } = "receivedAt-desc";
    public bool ShowPreview { get; set; } = true;
    public bool GroupByDate { get; set; } = false;

    // Notification preferences
    public bool DesktopNotifications { get; set; } = false;
    public bool EmailDigest { get; set; } = false;
    public string DigestFrequency { get; set; } = "daily";
    public TimeOnly DigestTime { get; set; } = new TimeOnly(9, 0);

    public DateTimeOffset UpdatedAt { get; set; }
}

public class UpdateUserPreferenceRequest
{
    // Display preferences
    public string? Theme { get; set; }
    public string? DisplayDensity { get; set; }
    public int? EmailsPerPage { get; set; }
    public string? DefaultSort { get; set; }
    public bool? ShowPreview { get; set; }
    public bool? GroupByDate { get; set; }

    // Notification preferences
    public bool? DesktopNotifications { get; set; }
    public bool? EmailDigest { get; set; }
    public string? DigestFrequency { get; set; }
    public TimeOnly? DigestTime { get; set; }
}
