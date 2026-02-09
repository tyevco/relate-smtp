using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Api.Models;

public record OutboundEmailListItemDto(
    Guid Id,
    string FromAddress,
    string? FromDisplayName,
    string Subject,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? SentAt,
    int RecipientCount,
    int AttachmentCount
);

public record OutboundEmailDetailDto(
    Guid Id,
    string FromAddress,
    string? FromDisplayName,
    string Subject,
    string? TextBody,
    string? HtmlBody,
    string Status,
    string? MessageId,
    string? InReplyTo,
    Guid? OriginalEmailId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? SentAt,
    int RetryCount,
    string? LastError,
    List<OutboundRecipientDto> Recipients,
    List<OutboundAttachmentDto> Attachments
);

public record OutboundRecipientDto(
    Guid Id,
    string Address,
    string? DisplayName,
    string Type,
    string Status,
    string? StatusMessage,
    DateTimeOffset? DeliveredAt
);

public record OutboundAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes
);

public record OutboundEmailListResponse(
    List<OutboundEmailListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record CreateDraftRequest
{
    public string FromAddress { get; init; } = string.Empty;
    public string? FromDisplayName { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public List<RecipientRequest> Recipients { get; init; } = new();
}

public record RecipientRequest
{
    public string Address { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string Type { get; init; } = "To";
}

public record UpdateDraftRequest
{
    public string? FromAddress { get; init; }
    public string? FromDisplayName { get; init; }
    public string? Subject { get; init; }
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public List<RecipientRequest>? Recipients { get; init; }
}

public record SendEmailRequest
{
    public string FromAddress { get; init; } = string.Empty;
    public string? FromDisplayName { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public List<RecipientRequest> Recipients { get; init; } = new();
}

public record ReplyRequest
{
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public bool ReplyAll { get; init; }
}

public record ForwardRequest
{
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public List<RecipientRequest> Recipients { get; init; } = new();
}

public static class OutboundEmailMappingExtensions
{
    public static OutboundEmailListItemDto ToListItemDto(this OutboundEmail email)
    {
        return new OutboundEmailListItemDto(
            email.Id,
            email.FromAddress,
            email.FromDisplayName,
            email.Subject,
            email.Status.ToString(),
            email.CreatedAt,
            email.QueuedAt,
            email.SentAt,
            email.Recipients.Count,
            email.Attachments.Count
        );
    }

    public static OutboundEmailDetailDto ToDetailDto(this OutboundEmail email)
    {
        return new OutboundEmailDetailDto(
            email.Id,
            email.FromAddress,
            email.FromDisplayName,
            email.Subject,
            email.TextBody,
            email.HtmlBody,
            email.Status.ToString(),
            email.MessageId,
            email.InReplyTo,
            email.OriginalEmailId,
            email.CreatedAt,
            email.QueuedAt,
            email.SentAt,
            email.RetryCount,
            email.LastError,
            email.Recipients.Select(r => new OutboundRecipientDto(
                r.Id,
                r.Address,
                r.DisplayName,
                r.Type.ToString(),
                r.Status.ToString(),
                r.StatusMessage,
                r.DeliveredAt
            )).ToList(),
            email.Attachments.Select(a => new OutboundAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes
            )).ToList()
        );
    }
}
