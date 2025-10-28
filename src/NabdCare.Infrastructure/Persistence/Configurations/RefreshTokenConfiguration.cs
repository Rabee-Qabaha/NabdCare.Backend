using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token).IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAdd();

        // NEW
        builder.Property(rt => rt.ClinicId).IsRequired(false);
        builder.Property(rt => rt.IssuedForUserAgent).HasMaxLength(500);

        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.ExpiresAt);

        builder.HasIndex(rt => rt.ClinicId);
    }
}
