using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class DeliveryLogConfiguration : IEntityTypeConfiguration<DeliveryLog>
{
    public void Configure(EntityTypeBuilder<DeliveryLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.RecipientAddress)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(l => l.MxHost)
            .HasMaxLength(500);

        builder.Property(l => l.SmtpResponse)
            .HasMaxLength(2000);

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(l => l.OutboundEmailId);
        builder.HasIndex(l => l.AttemptedAt);

        builder.HasOne(l => l.Recipient)
            .WithMany()
            .HasForeignKey(l => l.RecipientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
