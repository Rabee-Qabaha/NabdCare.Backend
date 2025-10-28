using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Audits;

namespace NabdCare.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(a => a.Id);

            // ------------------------------------------
            // ✅ Column properties
            // ------------------------------------------

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
                .HasColumnType("jsonb");

            builder.Property(a => a.Reason)
                .HasMaxLength(1000);

            builder.Property(a => a.IpAddress)
                .HasMaxLength(45); // IPv6 support

            builder.Property(a => a.UserAgent)
                .HasMaxLength(500);

            // ------------------------------------------
            // ✅ Indexes for fast querying
            // ------------------------------------------
            
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

            // ------------------------------------------
            // ✅ GIN index for JSON search performance
            // ------------------------------------------
            builder.HasIndex(a => a.Changes)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops")
                .HasDatabaseName("IX_AuditLogs_Changes_GIN");
        }
    }
}