using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class OutboundAttachmentConfiguration : IEntityTypeConfiguration<OutboundAttachment>
{
    public void Configure(EntityTypeBuilder<OutboundAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Content)
            .IsRequired();
    }
}
