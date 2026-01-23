using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Polymorphic integrity check
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_Payment_Context",
                "(\"Context\" = 0 AND \"ClinicId\" IS NOT NULL AND \"PatientId\" IS NULL) OR " +
                "(\"Context\" = 1 AND \"PatientId\" IS NOT NULL AND \"ClinicId\" IS NULL)"
            );
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.PaymentDate).IsRequired().HasDefaultValueSql("NOW()"); 
        builder.Property(p => p.Status).IsRequired().HasDefaultValue(PaymentStatus.Pending);

        // ============================================================
        // 🔗 RELATIONSHIPS
        // ============================================================

        // Clinic (Tenant)
        builder.HasOne(p => p.Clinic)
            .WithMany() // Clinic does NOT have a payments list
            .HasForeignKey(p => p.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient
        builder.HasOne(p => p.Patient)
            .WithMany() // Patient entity you provided did NOT have a 'Payments' list (only Invoices)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cheque Details (1-to-1)
        builder.HasOne(p => p.ChequeDetail)
            .WithOne(cd => cd.Payment)
            .HasForeignKey<ChequePaymentDetail>(cd => cd.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============================================================
        // ⚡ INDEXES
        // ============================================================
        builder.HasIndex(p => new { p.Context, p.ClinicId, p.PatientId });
        builder.HasIndex(p => new { p.ClinicId, p.PaymentDate });
        builder.HasIndex(p => new { p.PatientId, p.PaymentDate });
        builder.HasIndex(p => p.PaymentDate);
    }
}