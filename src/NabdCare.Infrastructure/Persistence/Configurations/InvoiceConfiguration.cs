using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        // ⚡ Indexes
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.ClinicId);
        builder.HasIndex(i => i.SubscriptionId);
        builder.HasIndex(i => i.IssueDate);
        
        // ✅ 2025: New Indexes
        builder.HasIndex(i => i.IdempotencyKey).IsUnique(); // Prevent duplicates
        builder.HasIndex(i => i.Currency); // For reporting

        // 💰 Precision
        builder.Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Property(i => i.TaxRate).HasPrecision(18, 4);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);

        // ✅ 2025: New Fields Configuration
        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(i => i.PdfUrl).HasMaxLength(500);
        builder.Property(i => i.HostedPaymentUrl).HasMaxLength(500);
        builder.Property(i => i.IdempotencyKey).HasMaxLength(100);

        // 🔒 Relationships
        builder.HasOne(i => i.Clinic)
            .WithMany()
            .HasForeignKey(i => i.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Subscription)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(item => item.Invoice)
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}