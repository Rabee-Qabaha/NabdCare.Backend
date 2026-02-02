using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", t =>
        {
            // Constraint: Invoice must act as EITHER SaaS Bill OR Patient Bill (not both, not neither)
            // Checks that SubscriptionId is set OR PatientId is set.
            t.HasCheckConstraint("CK_Invoice_Polymorphic", 
                "(\"SubscriptionId\" IS NOT NULL AND \"PatientId\" IS NULL) OR " +
                "(\"PatientId\" IS NOT NULL AND \"SubscriptionId\" IS NULL)"
            );
        });

        builder.HasKey(i => i.Id);

        // ============================================================
        // 💰 PRECISION & DEFAULTS
        // ============================================================
        builder.Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Property(i => i.TaxRate).HasPrecision(18, 4);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.IdempotencyKey).HasMaxLength(100);
        builder.Property(i => i.PdfUrl).HasMaxLength(500);
        builder.Property(i => i.HostedPaymentUrl).HasMaxLength(500);

        // ============================================================
        // 🔗 RELATIONSHIPS
        // ============================================================

        // Clinic Link (Cascade Delete OK for Tenant Cleanup)
        builder.HasOne(i => i.Clinic)
            .WithMany()
            .HasForeignKey(i => i.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        // Case A: SaaS Subscription
        builder.HasOne(i => i.Subscription)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Case B: Patient Bill
        builder.HasOne(i => i.Patient)
            .WithMany(p => p.Invoices)
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional Link: Clinical Encounter
        builder.HasOne(i => i.ClinicalEncounter)
            .WithMany() // No list on Encounter side
            .HasForeignKey(i => i.ClinicalEncounterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Invoice Items
        builder.HasMany(i => i.Items)
            .WithOne(item => item.Invoice)
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Payment Allocations (Replaces direct Payments list)
        builder.HasMany(i => i.PaymentAllocations)
             .WithOne(pa => pa.Invoice)
             .HasForeignKey(pa => pa.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

        // ============================================================
        // ⚡ INDEXES
        // ============================================================
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.IdempotencyKey).IsUnique();
        builder.HasIndex(i => i.ClinicId);
        builder.HasIndex(i => i.SubscriptionId); // For SaaS lookups
        builder.HasIndex(i => i.PatientId);      // For Patient History
        builder.HasIndex(i => i.IssueDate);
        builder.HasIndex(i => i.Status);         // For "Overdue" queries
    }
}