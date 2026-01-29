using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MessageId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.FromAddress)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(e => e.FromDisplayName)
            .HasMaxLength(500);

        builder.Property(e => e.Subject)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.InReplyTo)
            .HasMaxLength(500);

        builder.Property(e => e.References)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.MessageId);
        builder.HasIndex(e => e.ReceivedAt);
        builder.HasIndex(e => e.ThreadId);

        builder.HasMany(e => e.Recipients)
            .WithOne(r => r.Email)
            .HasForeignKey(r => r.EmailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.Email)
            .HasForeignKey(a => a.EmailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
