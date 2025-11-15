using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NabdCare.Application.Common;

namespace NabdCare.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NabdCareDbContext>
{
    public NabdCareDbContext CreateDbContext(string[] args)
    {
        // Build configuration to read appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get the connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<NabdCareDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // ✅ Use dummy implementations for design-time purposes
        var tenantContext = new DesignTimeTenantContext();
        var userContext = new DesignTimeUserContext();

        return new NabdCareDbContext(optionsBuilder.Options, tenantContext, userContext);
    }
}

/// <summary>
/// Dummy TenantContext implementation for EF Core design-time operations (migrations)
/// </summary>
public class DesignTimeTenantContext : ITenantContext
{
    public Guid? ClinicId => null;
    public Guid? UserId => null;
    public string? UserEmail => "system@nabdcare.local";
    public bool IsSuperAdmin => true; // Allow all operations during migrations
    public string? UserRole => "SuperAdmin";
    public Guid? RoleId { get; }
    public bool IsAuthenticated => false;
    
}

/// <summary>
/// Dummy UserContext implementation for EF Core design-time operations (migrations)
/// </summary>
public class DesignTimeUserContext : IUserContext
{
    public string GetCurrentUserId()
    {
        return "System";
    }

    public string? GetCurrentUserRoleId()
    {
        return null; // No role ID during design-time/migrations
    }

    public string? GetCurrentUserFullName()
    {
        return null;
    }

    public string? GetCurrentUserEmail()
    {
        return null;
    }
}