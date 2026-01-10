using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        // 1. Table & Key
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);

        // 2. Property Constraints (Database Limits)
        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100); // Reasonable limit for branch names

        builder.Property(b => b.Address)
            .HasMaxLength(255);

        builder.Property(b => b.Phone)
            .HasMaxLength(20);
            
        builder.Property(b => b.Email)
            .HasMaxLength(100);

        // 3. Relationships
        // When a Clinic is deleted, delete all its branches (Cascade)
        builder.HasOne(b => b.Clinic)
            .WithMany(c => c.Branches)
            .HasForeignKey(b => b.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        // 4. Performance Indexes
        // Used heavily when filtering branches by clinic
        builder.HasIndex(b => b.ClinicId);

        // 5. 🛑 CRITICAL LOGIC: Enforce "One Main Branch per Clinic"
        // This creates a Partial Unique Index. 
        // It says: "For any specific ClinicId, you can only have ONE row where IsMain is true."
        // We also check !IsDeleted so soft-deleted main branches don't block new ones.
        builder.HasIndex(b => b.ClinicId)
            .IsUnique()
            .HasFilter("\"IsMain\" = true AND \"IsDeleted\" = false");
    }
}