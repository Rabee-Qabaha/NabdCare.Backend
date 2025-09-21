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
            .AddJsonFile("appsettings.json")
            .Build();

        // Get the connection string (replace "DefaultConnection" with your actual name)
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<NabdCareDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Use a dummy TenantContext for migrations
        var tenantContext = new TenantContext();

        return new NabdCareDbContext(optionsBuilder.Options, tenantContext);
    }
}