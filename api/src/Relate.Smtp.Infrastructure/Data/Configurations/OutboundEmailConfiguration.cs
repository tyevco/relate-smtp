using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class OutboundEmailConfiguration : IEntityTypeConfiguration<OutboundEmail>
{
    public void Configure(EntityTypeBuilder<OutboundEmail> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromAddress)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(e => e.FromDisplayName)
            .HasMaxLength(500);

        builder.Property(e => e.Subject)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.MessageId)
            .HasMaxLength(500);

        builder.Property(e => e.InReplyTo)
            .HasMaxLength(500);

        builder.Property(e => e.References)
            .HasMaxLength(2000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.NextRetryAt });
        builder.HasIndex(e => e.CreatedAt);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Recipients)
            .WithOne(r => r.OutboundEmail)
            .HasForeignKey(r => r.OutboundEmailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.OutboundEmail)
            .HasForeignKey(a => a.OutboundEmailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.DeliveryLogs)
            .WithOne(l => l.OutboundEmail)
            .HasForeignKey(l => l.OutboundEmailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
