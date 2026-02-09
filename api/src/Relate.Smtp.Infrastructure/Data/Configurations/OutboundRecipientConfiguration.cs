using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class OutboundRecipientConfiguration : IEntityTypeConfiguration<OutboundRecipient>
{
    public void Configure(EntityTypeBuilder<OutboundRecipient> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Address)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(r => r.DisplayName)
            .HasMaxLength(500);

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.StatusMessage)
            .HasMaxLength(1000);

        builder.HasIndex(r => r.OutboundEmailId);
    }
}
