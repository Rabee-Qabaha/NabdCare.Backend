using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Audits;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.UserEmail)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.Changes)
            .HasColumnType("jsonb"); // PostgreSQL JSONB

        builder.Property(a => a.Reason)
            .HasMaxLength(1000);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        // Indexes for common queries
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(a => a.ClinicId)
            .HasDatabaseName("IX_AuditLogs_ClinicId");

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(a => new { a.ClinicId, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Clinic_Timestamp");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");
    }
}