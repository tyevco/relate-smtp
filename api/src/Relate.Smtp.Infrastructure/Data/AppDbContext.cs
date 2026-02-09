using Microsoft.EntityFrameworkCore;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Email> Emails => Set<Email>();
    public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();
    public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserEmailAddress> UserEmailAddresses => Set<UserEmailAddress>();
    public DbSet<SmtpApiKey> SmtpApiKeys => Set<SmtpApiKey>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<EmailLabel> EmailLabels => Set<EmailLabel>();
    public DbSet<EmailFilter> EmailFilters => Set<EmailFilter>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<OutboundEmail> OutboundEmails => Set<OutboundEmail>();
    public DbSet<OutboundRecipient> OutboundRecipients => Set<OutboundRecipient>();
    public DbSet<OutboundAttachment> OutboundAttachments => Set<OutboundAttachment>();
    public DbSet<DeliveryLog> DeliveryLogs => Set<DeliveryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
