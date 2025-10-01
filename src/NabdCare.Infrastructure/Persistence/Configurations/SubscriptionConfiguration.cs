using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartDate).IsRequired();
        builder.Property(s => s.EndDate).IsRequired();

        builder.Property(s => s.Fee)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(s => s.Type)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        builder.HasOne(s => s.Clinic)
            .WithMany()
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Payments)
            .WithOne()
            .HasForeignKey(p => p.ClinicId);


        // Indexes
        builder.HasIndex(s => s.ClinicId);
        builder.HasIndex(s => s.Status);
    }
}