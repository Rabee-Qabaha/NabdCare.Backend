using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        // Branding Limits
        builder.Property(c => c.Website).HasMaxLength(255);
        builder.Property(c => c.TaxNumber).HasMaxLength(50);
        builder.Property(c => c.RegistrationNumber).HasMaxLength(50);
        builder.Property(c => c.LogoUrl).HasMaxLength(500);

        // ✅ Map Settings to JSONB column (PostgreSQL)
        builder.OwnsOne(c => c.Settings, settings =>
        {
            settings.ToJson(); 
        });

        // ✅ Optimization: Filtered Index for Email (Fixes the Unique constraint issue)
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(c => c.Name);
        
        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(60);

        // ✅ UNIQUE INDEX on Slug
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}