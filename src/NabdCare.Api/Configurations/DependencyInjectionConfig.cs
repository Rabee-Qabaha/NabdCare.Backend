using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Services;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Auth;

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
        services.AddScoped<DbSeeder>(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<NabdCareDbContext>>();
            var tenantContext = new TenantContext { IsSuperAdmin = true };
            var dbContext = new NabdCareDbContext(options, tenantContext);
            return new DbSeeder(dbContext);
        });
        services.AddHostedService<DbSeedHostedService>();
        
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        

        return services;
    }
}