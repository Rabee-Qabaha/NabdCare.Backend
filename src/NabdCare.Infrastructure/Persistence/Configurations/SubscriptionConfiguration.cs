using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PlanId).IsRequired().HasMaxLength(50).HasDefaultValue("STD_M");
        builder.Property(s => s.StartDate).IsRequired();
        builder.Property(s => s.EndDate).IsRequired();
        builder.Property(s => s.Fee).IsRequired().HasPrecision(18, 2);
        builder.Property(s => s.Type).IsRequired();
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.AutoRenew).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.GracePeriodDays).IsRequired().HasDefaultValue(0);

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasConversion<string>();
        builder.Property(s => s.ExternalSubscriptionId).HasMaxLength(100);
        builder.Property(s => s.CancelAtPeriodEnd).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);
        builder.Property(s => s.BillingCycleAnchor).IsRequired(false);

        builder.Property(s => s.PurchasedBranches).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.IncludedBranchesSnapshot).IsRequired().HasDefaultValue(1);
        builder.Property(s => s.BonusBranches).IsRequired().HasDefaultValue(0);
        
        builder.Property(s => s.PurchasedUsers).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.IncludedUsersSnapshot).IsRequired().HasDefaultValue(1);
        builder.Property(s => s.BonusUsers).IsRequired().HasDefaultValue(0);

        builder.Ignore(s => s.MaxBranches);
        builder.Ignore(s => s.MaxUsers);

        builder.HasOne(s => s.Clinic)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.PreviousSubscriptionId);
        builder.HasIndex(s => s.ClinicId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CancelAtPeriodEnd);
        builder.HasIndex(s => s.ExternalSubscriptionId);
        builder.HasIndex(s => s.CreatedAt);

        builder.HasIndex(s => new { s.ClinicId, s.Status })
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)SubscriptionStatus.Future}");
    }
}