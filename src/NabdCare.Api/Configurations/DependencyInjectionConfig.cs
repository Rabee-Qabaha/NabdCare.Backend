using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Services;
using NabdCare.Application.Validator;
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

        // Add IHttpContextAccessor
        services.AddHttpContextAccessor();

        // Register TenantContext as Scoped
        services.AddScoped<ITenantContext, TenantContext>();
        // Register UserContext as Scoped
        services.AddScoped<IUserContext, UserContext>();

        // Register other services and repositories here
        services.AddScoped<DbSeeder>(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<NabdCareDbContext>>();
            var tenantContext = new TenantContext { IsSuperAdmin = true };
            var userContext = sp.GetRequiredService<IUserContext>();
            var dbContext = new NabdCareDbContext(options, tenantContext, userContext);
            return new DbSeeder(dbContext);
        });
        services.AddHostedService<DbSeedHostedService>();
        
        services.AddScoped<ITokenService, JwtTokenService>();

        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        

        return services;
    }
}