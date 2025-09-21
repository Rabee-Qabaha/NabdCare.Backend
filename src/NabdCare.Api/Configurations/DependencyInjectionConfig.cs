using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<NabdCareDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // Register TenantContext as Scoped
        services.AddScoped<ITenantContext, TenantContext>();

        // Register other services and repositories here
        services.AddScoped<DbSeeder>();
        services.AddHostedService<DbSeedHostedService>();

        return services;
    }
}