using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.ColorCode)
            .HasMaxLength(7);

        builder.Property(r => r.IconClass)
            .HasMaxLength(50);

        builder.Property(r => r.DisplayOrder)
            .HasDefaultValue(100);

        // Relationships
        builder.HasOne(r => r.Clinic)
            .WithMany()
            .HasForeignKey(r => r.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TemplateRole)
            .WithMany()
            .HasForeignKey(r => r.TemplateRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Users)
            .WithOne(u => u.Role)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict); // Cannot delete role with assigned users

        // Indexes
        // Unique constraint: Role name must be unique per clinic
        // System roles (ClinicId = NULL) must have unique names globally
        builder.HasIndex(r => new { r.Name, r.ClinicId })
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name_ClinicId");

        builder.HasIndex(r => r.ClinicId)
            .HasDatabaseName("IX_Roles_ClinicId");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");

        builder.HasIndex(r => r.IsTemplate)
            .HasDatabaseName("IX_Roles_IsTemplate");

        builder.HasIndex(r => r.DisplayOrder)
            .HasDatabaseName("IX_Roles_DisplayOrder");
    }
}