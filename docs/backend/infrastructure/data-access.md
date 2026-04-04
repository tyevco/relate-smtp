# Data Access

The Infrastructure layer uses Entity Framework Core with the Npgsql PostgreSQL provider for all database operations. This page covers the `AppDbContext`, entity configurations, indexes, and migration management.

## AppDbContext

`AppDbContext` is the central EF Core database context, exposing 16 `DbSet` properties:

| DbSet | Entity | Description |
|-------|--------|-------------|
| `Emails` | `Email` | Inbound email messages |
| `EmailRecipients` | `EmailRecipient` | Per-recipient delivery records |
| `EmailAttachments` | `EmailAttachment` | File attachments |
| `Users` | `User` | User accounts |
| `UserEmailAddresses` | `UserEmailAddress` | Additional email addresses |
| `SmtpApiKeys` | `SmtpApiKey` | API keys |
| `Labels` | `Label` | User labels |
| `EmailLabels` | `EmailLabel` | Email-to-label assignments |
| `EmailFilters` | `EmailFilter` | Automated filter rules |
| `UserPreferences` | `UserPreference` | User settings |
| `PushSubscriptions` | `PushSubscription` | Web push subscriptions |
| `OutboundEmails` | `OutboundEmail` | Outgoing emails |
| `OutboundRecipients` | `OutboundRecipient` | Per-recipient outbound status |
| `OutboundAttachments` | `OutboundAttachment` | Outbound attachments |
| `DeliveryLogs` | `DeliveryLog` | Delivery attempt records |

### Configuration Loading

Entity configurations are loaded automatically from the assembly:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

This scans the `Configurations/` directory for all classes implementing `IEntityTypeConfiguration<T>`.

## Entity Configurations

The `Data/Configurations/` directory contains 15 Fluent API configuration files that define primary keys, foreign key relationships, indexes, and column constraints for each entity:

| Configuration File | Entity |
|-------------------|--------|
| `EmailConfiguration.cs` | Email |
| `EmailRecipientConfiguration.cs` | EmailRecipient |
| `EmailAttachmentConfiguration.cs` | EmailAttachment |
| `UserConfiguration.cs` | User |
| `UserEmailAddressConfiguration.cs` | UserEmailAddress |
| `SmtpApiKeyConfiguration.cs` | SmtpApiKey |
| `LabelConfiguration.cs` | Label |
| `EmailLabelConfiguration.cs` | EmailLabel |
| `EmailFilterConfiguration.cs` | EmailFilter |
| `UserPreferenceConfiguration.cs` | UserPreference |
| `PushSubscriptionConfiguration.cs` | PushSubscription |
| `OutboundEmailConfiguration.cs` | OutboundEmail |
| `OutboundRecipientConfiguration.cs` | OutboundRecipient |
| `OutboundAttachmentConfiguration.cs` | OutboundAttachment |
| `DeliveryLogConfiguration.cs` | DeliveryLog |

### Key Indexes

The following database indexes are critical for query performance:

| Index | Column(s) | Type | Purpose |
|-------|-----------|------|---------|
| User OIDC lookup | `OidcSubject` + `OidcIssuer` | Unique | Fast OIDC authentication lookup |
| API key prefix | `KeyPrefix` | Non-unique | O(1) key lookup by prefix |
| Email Message-ID | `MessageId` | Non-unique | Deduplication and threading |
| EmailRecipient user | `UserId` | Non-unique | Inbox query (all emails for a user) |
| OutboundEmail queue | `UserId` + `Status` | Non-unique | Outbox/sent mail queries and delivery queue |

### Foreign Key Relationships

Key relationships configured in the Fluent API:

- `Email` has many `EmailRecipient` (cascade delete)
- `Email` has many `EmailAttachment` (cascade delete)
- `User` has many `SmtpApiKey` (cascade delete)
- `User` has many `Label` (cascade delete)
- `User` has one `UserPreference` (cascade delete)
- `OutboundEmail` has many `OutboundRecipient` (cascade delete)
- `OutboundEmail` has many `OutboundAttachment` (cascade delete)
- `OutboundEmail` has many `DeliveryLog` (cascade delete)

## Migrations

Four migrations track the database schema evolution:

| Migration | Date | Description |
|-----------|------|-------------|
| `InitialCreate` | 2026-01-31 | Base schema with Users, Emails, Recipients, Attachments, API Keys, Labels, Filters, Preferences, Push Subscriptions |
| `AddApiKeyPrefix` | 2026-02-06 | Adds `KeyPrefix` column to SmtpApiKey for efficient prefix-based lookup |
| `AddEmailVerificationToken` | 2026-02-09 | Adds verification token fields to UserEmailAddress |
| `AddOutboundEmailTables` | 2026-02-09 | Adds OutboundEmail, OutboundRecipient, OutboundAttachment, and DeliveryLog tables |

### Creating a New Migration

From the `api/` directory:

```bash
dotnet ef migrations add MigrationName \
    --project src/Relate.Smtp.Infrastructure \
    --startup-project src/Relate.Smtp.Api
```

The `--startup-project` flag is required because the `Infrastructure` project does not have a `Program.cs` -- it needs the API project to provide the host and configuration.

### Applying Migrations

Manually:

```bash
dotnet ef database update \
    --project src/Relate.Smtp.Infrastructure \
    --startup-project src/Relate.Smtp.Api
```

### Auto-Migration in Development

When the API starts in development mode (the `ASPNETCORE_ENVIRONMENT` is `Development`), it automatically applies pending migrations on startup. This means you do not need to manually run `database update` during local development -- just start the API and it will create or update the schema.

In production, migrations should be applied as part of the deployment process, either manually or through a CI/CD pipeline.
