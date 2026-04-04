# Core Domain Layer

The `Relate.Smtp.Core` project is the innermost layer of the application, following Clean Architecture principles. It contains the domain entities, repository interfaces, search models, and shared protocol utilities. Crucially, it has **zero external dependencies** -- no NuGet packages, no EF Core references, no infrastructure concerns.

## Purpose

The Core layer defines _what_ the application does without specifying _how_. It establishes:

- **Entities** -- The business objects that represent the domain (users, emails, labels, filters, etc.)
- **Repository interfaces** -- Contracts for data access that the Infrastructure layer implements
- **Models** -- Value objects and search filter structures used across the application
- **Protocol utilities** -- Shared base classes for POP3 and IMAP session management

## Project Structure

```
Relate.Smtp.Core/
  Entities/
    User.cs                    # User account
    Email.cs                   # Inbound email message
    EmailRecipient.cs          # Per-recipient delivery record
    EmailAttachment.cs         # File attachment
    EmailLabel.cs              # Email-to-label junction
    Label.cs                   # User-defined label
    EmailFilter.cs             # Automated filter rule
    SmtpApiKey.cs              # API key for protocol auth
    UserEmailAddress.cs        # Additional email addresses
    OutboundEmail.cs           # Outgoing email
    OutboundRecipient.cs       # Per-recipient outbound status
    OutboundAttachment.cs      # Outbound attachment
    UserPreference.cs          # User settings
    PushSubscription.cs        # Web push subscription
    DeliveryLog.cs             # Delivery attempt record
    RecipientType.cs           # To/Cc/Bcc enum
    OutboundEmailStatus.cs     # Draft/Queued/Sent/Failed enum
    OutboundRecipientStatus.cs # Pending/Sent/Failed/Deferred enum
  Interfaces/
    IEmailRepository.cs
    IUserRepository.cs
    ISmtpApiKeyRepository.cs
    ILabelRepository.cs
    IEmailLabelRepository.cs
    IEmailFilterRepository.cs
    IUserPreferenceRepository.cs
    IPushSubscriptionRepository.cs
    IOutboundEmailRepository.cs
  Models/
    EmailSearchFilters.cs      # Search query parameters
  Protocol/
    ProtocolSession.cs         # Base session class for POP3/IMAP
    BoundedStreamReader.cs     # DoS-resistant line reader
    ConnectionRegistry.cs      # Per-user connection tracking
```

## Dependency Direction

The dependency rule is strict and one-directional:

```
Infrastructure --> Core <-- Pop3Host, ImapHost, SmtpHost, Api
```

- Core depends on **nothing** (no project references, no NuGet packages).
- Every other project depends on Core.
- Infrastructure implements the interfaces defined in Core.
- The protocol hosts and API consume Core entities and interfaces.

This means the domain logic is completely isolated from database technology, HTTP frameworks, and protocol implementations. You can understand the business rules by reading Core alone, without needing to know about PostgreSQL, EF Core, or ASP.NET.

## Key Design Decisions

### No EF Core Annotations

Entity classes are plain C# classes (POCOs) with no `[Key]`, `[Required]`, or other EF Core attributes. All database mapping is handled by Fluent API configurations in the Infrastructure layer. This keeps Core free of ORM dependencies.

### Navigation Properties

Some entities include navigation properties (e.g., `Email.Recipients`) that EF Core populates when loading from the database. These are typed as `ICollection<T>` and initialized with empty lists to prevent null reference issues when entities are created in memory.

### Async Interfaces

All repository methods are async (`Task<T>`) and accept `CancellationToken` for cooperative cancellation. This supports the server's need to handle many concurrent connections efficiently.

## Further Reading

- [Entities](./entities.md) -- Detailed documentation of all 18 domain entities
- [Repository Interfaces](./interfaces.md) -- Method signatures and contracts for data access
- [Models](./models.md) -- Search filters and DTOs
- [Protocol Utilities](./protocol.md) -- Shared base classes for POP3 and IMAP
