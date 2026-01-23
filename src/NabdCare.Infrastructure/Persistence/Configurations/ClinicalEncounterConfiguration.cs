using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Clinical;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class ClinicalEncounterConfiguration : IEntityTypeConfiguration<ClinicalEncounter>
{
    public void Configure(EntityTypeBuilder<ClinicalEncounter> builder)
    {
        // ============================================================
        // 1. Table Mapping
        // ============================================================
        builder.ToTable("ClinicalEncounters");

        // ============================================================
        // 2. Relationships (The Fix for "AppointmentId1")
        // ============================================================
        
        // Fix for the shadow property issue.
        // We explicitly tell EF: "This Appointment navigation matches the AppointmentId FK"
        builder.HasOne(e => e.Appointment)
               .WithMany() // Assuming Appointment doesn't have a list of Encounters back to it
               .HasForeignKey(e => e.AppointmentId)
               .OnDelete(DeleteBehavior.SetNull); // If appointment deleted, keep the medical record

        builder.HasOne(e => e.Clinic)
               .WithMany()
               .HasForeignKey(e => e.ClinicId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Patient)
               .WithMany(p => p.Encounters)
               .HasForeignKey(e => e.PatientId)
               .OnDelete(DeleteBehavior.Restrict); // Don't delete patient if they have records

        builder.HasOne(e => e.Doctor)
               .WithMany()
               .HasForeignKey(e => e.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // 3. Column Configurations
        // ============================================================
        
        // Standard text columns
        builder.Property(e => e.ChiefComplaint).HasMaxLength(2000);
        builder.Property(e => e.Diagnosis).HasMaxLength(2000);
        builder.Property(e => e.TreatmentPlan).HasMaxLength(4000);

        // ============================================================
        // 4. JSONB Mapping (PostgreSQL / EF Core 7+)
        // ============================================================
        // This allows you to query inside the JSON (e.g., e.SpecialtyData.ToothId)
        // Note: For this to work with .ToJson(), the property type in the Entity
        // usually needs to be a class/record, not just a string. 
        // 
        // SINCE your entity currently defines them as 'string', we use standard column mapping:
        
        builder.Property(e => e.SpecialtyData)
               .HasColumnType("jsonb");

        builder.Property(e => e.VitalsSnapshot)
               .HasColumnType("jsonb");

        // ============================================================
        // 5. Indexes for Performance
        // ============================================================
        builder.HasIndex(e => e.ClinicId);
        builder.HasIndex(e => e.PatientId);
        builder.HasIndex(e => e.Date); // Critical for "Patient Timeline" queries
    }
}