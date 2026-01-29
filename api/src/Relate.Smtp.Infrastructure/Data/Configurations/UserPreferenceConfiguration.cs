using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Theme)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.DisplayDensity)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.EmailsPerPage)
            .IsRequired();

        builder.Property(p => p.DefaultSort)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.DigestFrequency)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Unique index on UserId (one preference per user)
        builder.HasIndex(p => p.UserId)
            .IsUnique();

        // Relationship with User
        builder.HasOne(p => p.User)
            .WithOne(u => u.Preference)
            .HasForeignKey<UserPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
