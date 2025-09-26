using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Application.Services;
using NabdCare.Application.Services.Auth;
using NabdCare.Application.Services.Permissions;
using NabdCare.Application.Services.Users;
using NabdCare.Application.Validator;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Auth;
using NabdCare.Infrastructure.Repositories.Permissions;
using NabdCare.Infrastructure.Repositories.Users;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<NabdCareDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // HttpContextAccessor
        services.AddHttpContextAccessor();

        // Tenant & User context
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUserContext, UserContext>();

        // Auth services
        services.AddScoped<IPasswordService, IdentityPasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // User services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        // Permission services
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionService, PermissionService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // Seeder
        services.AddScoped<DbSeeder>();
        services.AddHostedService<DbSeedHostedService>();

        return services;
    }
}