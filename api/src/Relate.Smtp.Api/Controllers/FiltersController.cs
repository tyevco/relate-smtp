using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Core.Entities;
using Relate.Smtp.Core.Interfaces;

namespace Relate.Smtp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FiltersController : ControllerBase
{
    private readonly IEmailFilterRepository _filterRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly UserProvisioningService _userProvisioningService;
    private readonly EmailFilterService _filterService;

    public FiltersController(
        IEmailFilterRepository filterRepository,
        IEmailRepository emailRepository,
        UserProvisioningService userProvisioningService,
        EmailFilterService filterService)
    {
        _filterRepository = filterRepository;
        _emailRepository = emailRepository;
        _userProvisioningService = userProvisioningService;
        _filterService = filterService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmailFilterDto>>> GetFilters(CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var filters = await _filterRepository.GetByUserIdAsync(user.Id, cancellationToken);
        return Ok(filters.Select(f => f.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<EmailFilterDto>> CreateFilter(
        [FromBody] CreateEmailFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);

        var filter = new EmailFilter
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            IsEnabled = request.IsEnabled ?? true,
            Priority = request.Priority ?? 100,
            FromAddressContains = request.FromAddressContains,
            SubjectContains = request.SubjectContains,
            BodyContains = request.BodyContains,
            HasAttachments = request.HasAttachments,
            MarkAsRead = request.MarkAsRead ?? false,
            AssignLabelId = request.AssignLabelId,
            Delete = request.Delete ?? false,
            CreatedAt = DateTimeOffset.UtcNow,
            TimesApplied = 0
        };

        await _filterRepository.AddAsync(filter, cancellationToken);
        return CreatedAtAction(nameof(GetFilters), new { id = filter.Id }, filter.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmailFilterDto>> UpdateFilter(
        Guid id,
        [FromBody] UpdateEmailFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var filter = await _filterRepository.GetByIdAsync(id, cancellationToken);

        if (filter == null || filter.UserId != user.Id)
        {
            return NotFound();
        }

        if (request.Name != null)
            filter.Name = request.Name;

        if (request.IsEnabled.HasValue)
            filter.IsEnabled = request.IsEnabled.Value;

        if (request.Priority.HasValue)
            filter.Priority = request.Priority.Value;

        if (request.FromAddressContains != null)
            filter.FromAddressContains = request.FromAddressContains;

        if (request.SubjectContains != null)
            filter.SubjectContains = request.SubjectContains;

        if (request.BodyContains != null)
            filter.BodyContains = request.BodyContains;

        if (request.HasAttachments.HasValue)
            filter.HasAttachments = request.HasAttachments;

        if (request.MarkAsRead.HasValue)
            filter.MarkAsRead = request.MarkAsRead.Value;

        if (request.AssignLabelId.HasValue)
            filter.AssignLabelId = request.AssignLabelId;

        if (request.Delete.HasValue)
            filter.Delete = request.Delete.Value;

        await _filterRepository.UpdateAsync(filter, cancellationToken);
        return Ok(filter.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFilter(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var filter = await _filterRepository.GetByIdAsync(id, cancellationToken);

        if (filter == null || filter.UserId != user.Id)
        {
            return NotFound();
        }

        await _filterRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/test")]
    public async Task<ActionResult<FilterTestResult>> TestFilter(
        Guid id,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate limit to prevent excessive resource usage
        if (limit < 1 || limit > 100)
        {
            return BadRequest("Limit must be between 1 and 100");
        }

        var user = await _userProvisioningService.GetOrCreateUserAsync(User, cancellationToken);
        var filter = await _filterRepository.GetByIdAsync(id, cancellationToken);

        if (filter == null || filter.UserId != user.Id)
        {
            return NotFound();
        }

        // Get recent emails for testing
        var emails = await _emailRepository.GetByUserIdAsync(user.Id, 0, limit, cancellationToken);

        var matchedEmailIds = new List<string>();
        foreach (var email in emails)
        {
            if (await EmailMatchesFilterAsync(email, filter))
            {
                matchedEmailIds.Add(email.Id.ToString());
            }
        }

        return Ok(new FilterTestResult(matchedEmailIds.Count, matchedEmailIds));
    }

    private async Task<bool> EmailMatchesFilterAsync(Email email, EmailFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.FromAddressContains))
        {
            if (!email.FromAddress.Contains(filter.FromAddressContains, StringComparison.OrdinalIgnoreCase) &&
                (email.FromDisplayName == null || !email.FromDisplayName.Contains(filter.FromAddressContains, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(filter.SubjectContains))
        {
            if (email.Subject == null || !email.Subject.Contains(filter.SubjectContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(filter.BodyContains))
        {
            var bodyText = email.TextBody ?? email.HtmlBody ?? string.Empty;
            if (!bodyText.Contains(filter.BodyContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (filter.HasAttachments.HasValue)
        {
            var hasAttachments = email.Attachments.Any();
            if (hasAttachments != filter.HasAttachments.Value)
            {
                return false;
            }
        }

        return await Task.FromResult(true);
    }
}

public record EmailFilterDto(
    Guid Id,
    string Name,
    bool IsEnabled,
    int Priority,
    string? FromAddressContains,
    string? SubjectContains,
    string? BodyContains,
    bool? HasAttachments,
    bool MarkAsRead,
    Guid? AssignLabelId,
    string? AssignLabelName,
    string? AssignLabelColor,
    bool Delete,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastAppliedAt,
    int TimesApplied
);

public record CreateEmailFilterRequest(
    string Name,
    bool? IsEnabled,
    int? Priority,
    string? FromAddressContains,
    string? SubjectContains,
    string? BodyContains,
    bool? HasAttachments,
    bool? MarkAsRead,
    Guid? AssignLabelId,
    bool? Delete
);

public record UpdateEmailFilterRequest(
    string? Name,
    bool? IsEnabled,
    int? Priority,
    string? FromAddressContains,
    string? SubjectContains,
    string? BodyContains,
    bool? HasAttachments,
    bool? MarkAsRead,
    Guid? AssignLabelId,
    bool? Delete
);

public record FilterTestResult(int MatchCount, List<string> MatchedEmailIds);

public static class EmailFilterExtensions
{
    public static EmailFilterDto ToDto(this EmailFilter filter)
    {
        return new EmailFilterDto(
            filter.Id,
            filter.Name,
            filter.IsEnabled,
            filter.Priority,
            filter.FromAddressContains,
            filter.SubjectContains,
            filter.BodyContains,
            filter.HasAttachments,
            filter.MarkAsRead,
            filter.AssignLabelId,
            filter.AssignLabel?.Name,
            filter.AssignLabel?.Color,
            filter.Delete,
            filter.CreatedAt,
            filter.LastAppliedAt,
            filter.TimesApplied
        );
    }
}
