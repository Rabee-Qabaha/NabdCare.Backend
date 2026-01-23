using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Scheduling;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        // 1. Bi-directional: Patient <-> Appointments
        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments) // Explicitly map to list
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Bi-directional: Doctor (User) <-> Appointments
        builder.HasOne(a => a.Doctor)
            .WithMany(u => u.Appointments) // Explicitly map to list
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // 3. 1-to-1: Appointment <-> ClinicalEncounter
        // Note: You must decide which side holds the FK. 
        // In your entity, ClinicalEncounter has AppointmentId.
        builder.HasOne<NabdCare.Domain.Entities.Clinical.ClinicalEncounter>() 
            .WithOne(e => e.Appointment)
            .HasForeignKey<NabdCare.Domain.Entities.Clinical.ClinicalEncounter>(e => e.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}