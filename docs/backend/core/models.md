# Models

The Core layer defines search filter models and the API layer defines DTOs (Data Transfer Objects) for REST responses. This page covers both.

## EmailSearchFilters

`EmailSearchFilters` defines the parameters for searching emails within a user's inbox. All fields are nullable -- a null field means "don't filter on this criterion."

```csharp
public class EmailSearchFilters
{
    public string? Query { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public bool? HasAttachments { get; set; }
    public bool? IsRead { get; set; }
}
```

| Field | Description |
|-------|-------------|
| `Query` | Full-text search across From address, Subject, and Body fields. The implementation uses SQL `ILIKE` for case-insensitive matching. |
| `FromDate` | Include only emails received on or after this date. |
| `ToDate` | Include only emails received on or before this date. |
| `HasAttachments` | When `true`, only emails with attachments. When `false`, only emails without. |
| `IsRead` | When `true`, only read emails. When `false`, only unread. |

These filters are used by both the `EmailsController` and `ExternalEmailsController` search endpoints, and are passed through to `IEmailRepository.SearchByUserIdAsync`.

## API DTOs

The API project (`Relate.Smtp.Api/Models/`) defines DTOs that shape the REST API responses. These are separate from the Core entities to decouple the API contract from the database schema.

### EmailListItemDto

A compact representation of an email for list views:

```csharp
public record EmailListItemDto(
    Guid Id,
    string MessageId,
    string FromAddress,
    string? FromDisplayName,
    string Subject,
    DateTimeOffset ReceivedAt,
    long SizeBytes,
    bool IsRead,
    int AttachmentCount
);
```

### EmailDetailDto

Full email details including body and recipient/attachment lists:

```csharp
public record EmailDetailDto(
    Guid Id, string MessageId, string FromAddress, string? FromDisplayName,
    string Subject, string? TextBody, string? HtmlBody,
    DateTimeOffset ReceivedAt, long SizeBytes, bool IsRead,
    List<EmailRecipientDto> Recipients,
    List<EmailAttachmentDto> Attachments
);
```

### EmailListResponse

Paginated list response with inbox statistics:

```csharp
public record EmailListResponse(
    List<EmailListItemDto> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize
);
```

### LabelDto

Label data for the labels management UI. Defined in `LabelDto.cs`.

### OutboundEmailDto

Outbound email data for the compose, drafts, outbox, and sent views. Defined in `OutboundEmailDto.cs`.

### ProfileDto

User profile data including primary email and additional addresses. Defined in `ProfileDto.cs`.

### PushSubscriptionDto

Push subscription data for managing notification subscriptions. Defined in `PushSubscriptionDto.cs`.

### UserPreferenceDto

User preference data for the settings UI. Defined in `UserPreferenceDto.cs`.

### SmtpApiKeyDto

API key metadata (never includes the raw key) for the credentials management UI:

```csharp
public record SmtpApiKeyDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    IReadOnlyList<string> Scopes
);
```

### API Key Scopes

The `ApiKeyScopes` class defines the valid permission scopes:

| Scope | Description |
|-------|-------------|
| `smtp` | SMTP server authentication |
| `pop3` | POP3 server authentication |
| `imap` | IMAP server authentication |
| `api:read` | REST API read access (external) |
| `api:write` | REST API write access (external) |
| `app` | Mobile/desktop app access |

### Mapping Extensions

`EmailMappingExtensions` provides `ToListItemDto()` and `ToDetailDto()` extension methods on the `Email` entity. These handle the mapping from domain entities to DTOs, including resolving per-user read status from the `EmailRecipient` records.
