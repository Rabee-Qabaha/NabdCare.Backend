﻿// Infrastructure/Persistence/Configurations/AppPermissionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.Configurations;

public class AppPermissionConfiguration : IEntityTypeConfiguration<AppPermission>
{
    public void Configure(EntityTypeBuilder<AppPermission> builder)
    {
        builder.ToTable("AppPermissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100); 

        builder.Property(p => p.Description)
            .HasMaxLength(500); 

        // Indexes
        builder.HasIndex(p => p.Name)
            .IsUnique();
    }
}