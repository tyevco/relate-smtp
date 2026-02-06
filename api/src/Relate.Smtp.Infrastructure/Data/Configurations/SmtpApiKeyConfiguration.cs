using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class SmtpApiKeyConfiguration : IEntityTypeConfiguration<SmtpApiKey>
{
    public void Configure(EntityTypeBuilder<SmtpApiKey> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(k => k.KeyHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(k => k.KeyPrefix)
            .HasMaxLength(12);

        builder.Property(k => k.Scopes)
            .HasMaxLength(500)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.HasIndex(k => k.UserId);
        builder.HasIndex(k => k.RevokedAt);

        // Composite index for efficient API key lookup by prefix
        builder.HasIndex(k => new { k.KeyPrefix, k.RevokedAt })
            .HasFilter("\"RevokedAt\" IS NULL");

        builder.HasOne(k => k.User)
            .WithMany(u => u.SmtpApiKeys)
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
