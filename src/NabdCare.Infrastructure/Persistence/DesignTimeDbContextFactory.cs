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

        // Get the connection string (replace "DefaultConnection" with your actual name)
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<NabdCareDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Use dummy TenantContext and UserContext for design-time purposes
        var tenantContext = new TenantContext();
        var userContext = new DummyUserContext();

        return new NabdCareDbContext(optionsBuilder.Options, tenantContext, userContext);
    }
}

// Dummy implementation of IUserContext for design-time operations
public class DummyUserContext : IUserContext
{
    public string GetCurrentUserId()
    {
        return "System"; // Return a default user ID for design-time purposes
    }
}