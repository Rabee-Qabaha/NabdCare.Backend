using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class AppPermissionConfiguration : IEntityTypeConfiguration<AppPermission>
{
    public void Configure(EntityTypeBuilder<AppPermission> builder)
    {
        builder.ToTable("AppPermissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.Description)
            .HasMaxLength(255);

        // Index
        builder.HasIndex(p => p.Name).IsUnique();
    }
}