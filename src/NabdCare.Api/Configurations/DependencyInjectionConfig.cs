using NabdCare.Application.Common;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services)
    {
        // Register TenantContext as Scoped
        services.AddScoped<ITenantContext, TenantContext>();

        // Register other services and repositories here
        services.AddScoped<DbSeeder>();
        services.AddHostedService<DbSeedHostedService>();
        
        return services;
    }
}