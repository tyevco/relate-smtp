using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Endpoint)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(s => s.P256dhKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.AuthKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.UserAgent)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Index on UserId for fast lookup
        builder.HasIndex(s => s.UserId);

        // Relationship with User
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
