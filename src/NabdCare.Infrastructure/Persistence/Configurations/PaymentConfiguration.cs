using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Use the table builder for the check constraint (non-obsolete)
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_Payment_Context",
                "(\"Context\" = 0 AND \"ClinicId\" IS NOT NULL AND \"PatientId\" IS NULL) OR " +
                "(\"Context\" = 1 AND \"PatientId\" IS NOT NULL AND \"ClinicId\" IS NULL)"
            );
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentDate)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.Context)
            .IsRequired();

        builder.Property(p => p.Method)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasDefaultValue(PaymentStatus.Pending);

        // Relations
        builder.HasOne(p => p.Clinic)
            .WithMany()
            .HasForeignKey(p => p.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ChequeDetail)
            .WithOne(cd => cd.Payment)
            .HasForeignKey<ChequePaymentDetail>(cd => cd.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for frequent queries
        builder.HasIndex(p => new { p.Context, p.ClinicId, p.PatientId });
        builder.HasIndex(p => new { p.ClinicId, p.PaymentDate });
        builder.HasIndex(p => new { p.PatientId, p.PaymentDate });
        builder.HasIndex(p => p.PaymentDate);
        builder.HasIndex(p => p.ClinicId);
        builder.HasIndex(p => p.PatientId);
    }
}
