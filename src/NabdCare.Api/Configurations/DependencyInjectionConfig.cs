using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Application.Mappings;
using NabdCare.Application.Services;
using NabdCare.Application.Services.Auth;
using NabdCare.Application.Services.Clinics;
using NabdCare.Application.Services.Permissions;
using NabdCare.Application.Services.Roles;
using NabdCare.Application.Services.Users;
using NabdCare.Application.Validator.Users;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Auth;
using NabdCare.Infrastructure.Repositories.Clinics;
using NabdCare.Infrastructure.Repositories.Permissions;
using NabdCare.Infrastructure.Repositories.Roles;
using NabdCare.Infrastructure.Repositories.Users;
using Microsoft.AspNetCore.Authorization;
using NabdCare.Api.Authorization;
using NabdCare.Application.Interfaces.Audit;
using NabdCare.Application.mappings;
using NabdCare.Infrastructure.Repositories.Audit;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<NabdCareDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // HttpContext Accessor
        services.AddHttpContextAccessor();

        // Tenant & User contexts
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUserContext, UserContext>();

        // Auth
        services.AddScoped<IPasswordService, IdentityPasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // Users
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        // Permissions
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionService, PermissionService>();

        // Roles
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleService, RoleService>();

        // RBAC Authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Clinic
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicService, ClinicService>();

        // Subscriptions
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // Audit
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        
        // AutoMapper
        services.AddAutoMapper(cfg => { },
            typeof(UserProfile).Assembly,
            typeof(ClinicProfile).Assembly,
            typeof(PermissionProfile).Assembly,
            typeof(SubscriptionProfile).Assembly,
            typeof(RoleProfile).Assembly
        );

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // Seeders
        services.AddScoped<DbSeeder>();
        services.AddScoped<ISingleSeeder, RolesSeeder>();
        services.AddScoped<ISingleSeeder, PermissionsSeeder>();
        services.AddScoped<ISingleSeeder, RolePermissionsSeeder>();
        services.AddScoped<ISingleSeeder, SuperAdminSeeder>();

        return services;
    }
}
