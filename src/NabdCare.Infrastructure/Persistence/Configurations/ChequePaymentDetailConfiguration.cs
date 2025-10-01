using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Payments;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class ChequePaymentDetailConfiguration : IEntityTypeConfiguration<ChequePaymentDetail>
{
    public void Configure(EntityTypeBuilder<ChequePaymentDetail> builder)
    {
        builder.ToTable("ChequePaymentDetails");

        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.ChequeNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cd => cd.BankName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cd => cd.Branch)
            .HasMaxLength(100);

        builder.Property(cd => cd.Amount)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(cd => cd.ChequeNumber).IsUnique();
        builder.HasIndex(cd => cd.Status);
    }
}