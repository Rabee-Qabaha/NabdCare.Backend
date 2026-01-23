using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        // ============================================================
        // 🔗 RELATIONSHIPS
        // ============================================================

        // Explicitly map back to Clinic.Users to avoid 'ClinicId1'
        builder.HasOne(u => u.Clinic)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        // Role relationship
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-Referencing (CreatedBy)
        builder.HasOne(u => u.CreatedByUser)
            .WithMany() // No "CreatedUsers" list in User entity
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Permissions
        builder.HasMany(u => u.Permissions)
            .WithOne(up => up.User)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Schedules (Doctor Availability)
        builder.HasMany(u => u.Schedules)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Appointments (Doctor's Calendar)
        builder.HasMany(u => u.Appointments)
            .WithOne(a => a.Doctor)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // ⚡ INDEXES
        // ============================================================
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.FullName);
        builder.HasIndex(u => u.RoleId);
        builder.HasIndex(u => u.ClinicId);
    }
}