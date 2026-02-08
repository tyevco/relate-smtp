using Microsoft.Extensions.Logging;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Services;

/// <summary>
/// Service for matching emails against filters and applying actions.
/// </summary>
public class EmailFilterService
{
    private readonly IEmailFilterRepository _filterRepository;
    private readonly IEmailLabelRepository _emailLabelRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger<EmailFilterService> _logger;

    public EmailFilterService(
        IEmailFilterRepository filterRepository,
        IEmailLabelRepository emailLabelRepository,
        IEmailRepository emailRepository,
        ILogger<EmailFilterService> logger)
    {
        _filterRepository = filterRepository;
        _emailLabelRepository = emailLabelRepository;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    /// <summary>
    /// Apply all enabled filters for a user to a specific email.
    /// </summary>
    public async Task ApplyFiltersToEmailAsync(Email email, Guid userId, CancellationToken cancellationToken = default)
    {
        var filters = await _filterRepository.GetEnabledByUserIdAsync(userId, cancellationToken);

        foreach (var filter in filters)
        {
            if (EmailMatchesFilter(email, filter))
            {
                await ApplyFilterActionsAsync(email, userId, filter, cancellationToken);

                // Update filter statistics
                filter.LastAppliedAt = DateTimeOffset.UtcNow;
                filter.TimesApplied++;
                await _filterRepository.UpdateAsync(filter, cancellationToken);

                _logger.LogInformation(
                    "Applied filter '{FilterName}' to email {EmailId} for user {UserId}",
                    filter.Name, email.Id, userId);
            }
        }
    }

    /// <summary>
    /// Check if an email matches a filter's conditions.
    /// This is a synchronous operation since all conditions are evaluated in memory.
    /// </summary>
    public bool EmailMatchesFilter(Email email, EmailFilter filter)
    {
        // Check FromAddress condition
        if (!string.IsNullOrEmpty(filter.FromAddressContains))
        {
            if (!email.FromAddress.Contains(filter.FromAddressContains, StringComparison.OrdinalIgnoreCase) &&
                (email.FromDisplayName == null || !email.FromDisplayName.Contains(filter.FromAddressContains, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        // Check Subject condition
        if (!string.IsNullOrEmpty(filter.SubjectContains))
        {
            if (email.Subject == null || !email.Subject.Contains(filter.SubjectContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check Body condition
        if (!string.IsNullOrEmpty(filter.BodyContains))
        {
            var bodyText = email.TextBody ?? email.HtmlBody ?? string.Empty;
            if (!bodyText.Contains(filter.BodyContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check HasAttachments condition
        if (filter.HasAttachments.HasValue)
        {
            var hasAttachments = email.Attachments.Any();
            if (hasAttachments != filter.HasAttachments.Value)
            {
                return false;
            }
        }

        return true;
    }

    private async Task ApplyFilterActionsAsync(Email email, Guid userId, EmailFilter filter, CancellationToken cancellationToken)
    {
        // Find the recipient for this user
        var recipient = email.Recipients.FirstOrDefault(r => r.UserId == userId);
        if (recipient == null)
        {
            _logger.LogWarning("No recipient found for user {UserId} in email {EmailId}", userId, email.Id);
            return;
        }

        // Action: Mark as Read
        if (filter.MarkAsRead)
        {
            recipient.IsRead = true;
        }

        // Action: Assign Label
        if (filter.AssignLabelId.HasValue)
        {
            try
            {
                var emailLabel = new EmailLabel
                {
                    Id = Guid.NewGuid(),
                    EmailId = email.Id,
                    UserId = userId,
                    LabelId = filter.AssignLabelId.Value,
                    AssignedAt = DateTimeOffset.UtcNow
                };

                await _emailLabelRepository.AddAsync(emailLabel, cancellationToken);
            }
#pragma warning disable CA1031 // Do not catch general exception types - Label assignment may fail due to various DB errors
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // Label might already be assigned, log and continue
                _logger.LogDebug(ex, "Failed to assign label to email (may already exist)");
            }
        }

        // Action: Delete
        if (filter.Delete)
        {
            _logger.LogInformation("Filter '{FilterName}' deleting email {EmailId}", filter.Name, email.Id);
            await _emailRepository.DeleteAsync(email.Id, cancellationToken);
        }
    }
}
