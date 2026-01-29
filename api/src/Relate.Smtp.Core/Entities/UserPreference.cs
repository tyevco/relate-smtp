namespace Relate.Smtp.Core.Entities;

/// <summary>
/// Represents user-specific preferences and settings.
/// </summary>
public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Display preferences
    public string Theme { get; set; } = "system"; // light, dark, system
    public string DisplayDensity { get; set; } = "comfortable"; // compact, comfortable, spacious
    public int EmailsPerPage { get; set; } = 20;
    public string DefaultSort { get; set; } = "receivedAt-desc";
    public bool ShowPreview { get; set; } = true;
    public bool GroupByDate { get; set; } = false;

    // Notification preferences
    public bool DesktopNotifications { get; set; } = false;
    public bool EmailDigest { get; set; } = false;
    public string DigestFrequency { get; set; } = "daily"; // daily, weekly
    public TimeOnly DigestTime { get; set; } = new TimeOnly(9, 0); // 9:00 AM

    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
