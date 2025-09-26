using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Services;
using NabdCare.Application.Services.Auth;
using NabdCare.Application.Validator;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Auth;
using NabdCare.Infrastructure.Repositories.Permissions;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext as scoped
        services.AddDbContext<NabdCareDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // HttpContextAccessor for accessing context
        services.AddHttpContextAccessor();

        // Tenant & User context
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUserContext, UserContext>();

        // Password service abstraction
        services.AddScoped<IPasswordService, IdentityPasswordService>();

        // Token service / auth related
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // Seeding: DbSeeder as scoped (or maybe singleton but probably scoped)
        services.AddScoped<DbSeeder>();

        // Optionally use an IHostedService to trigger seeding OR run it in Program.cs before app.Run()
        services.AddHostedService<DbSeedHostedService>();

        services.AddScoped<IPermissionRepository, PermissionRepository>();


        return services;
    }
}
