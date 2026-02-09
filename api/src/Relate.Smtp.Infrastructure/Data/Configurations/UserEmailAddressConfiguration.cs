using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relate.Smtp.Core.Entities;

namespace Relate.Smtp.Infrastructure.Data.Configurations;

public class UserEmailAddressConfiguration : IEntityTypeConfiguration<UserEmailAddress>
{
    public void Configure(EntityTypeBuilder<UserEmailAddress> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Address)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(a => a.VerificationToken)
            .HasMaxLength(6);

        builder.HasIndex(a => a.Address);
        builder.HasIndex(a => new { a.UserId, a.Address }).IsUnique();
    }
}
