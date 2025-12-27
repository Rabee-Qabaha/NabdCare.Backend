using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");
        builder.HasKey(item => item.Id);

        // 💰 Money Precision
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.Total).HasPrecision(18, 2);

        // Description Length Constraint
        builder.Property(item => item.Description)
            .IsRequired()
            .HasMaxLength(255);
    }
}