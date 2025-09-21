using NabdCare.Application.Common;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services)
    {
        // Register TenantContext as Scoped
        services.AddScoped<ITenantContext, TenantContext>();

        // Register other services and repositories here
        // services.AddScoped<IClinicRepository, ClinicRepository>();
        // services.AddScoped<IUserService, UserService>();
        // ...

        return services;
    }
}