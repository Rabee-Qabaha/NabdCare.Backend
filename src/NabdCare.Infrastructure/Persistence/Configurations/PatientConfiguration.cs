using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Patients;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        // 1. Bi-directional: Clinic <-> Patients
        builder.HasOne(p => p.Clinic)
            .WithMany(c => c.Patients) // Matches public ICollection<Patient> Patients
            .HasForeignKey(p => p.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Encounters List
        builder.HasMany(p => p.Encounters)
            .WithOne(e => e.Patient)
            .HasForeignKey(e => e.PatientId);

        // 3. Invoices List
        builder.HasMany(p => p.Invoices)
            .WithOne(i => i.Patient)
            .HasForeignKey(i => i.PatientId);
    }
}