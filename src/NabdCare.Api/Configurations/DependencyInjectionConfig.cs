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
using NabdCare.Application.mappings;
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
        
        // ✅ ADD THESE TWO LINES - Role services
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleService, RoleService>();
        
        // Role repository (already exists, keep it)
        services.AddScoped<IRoleRepository, RoleRepository>();
        
        // Clinic services
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicService, ClinicService>();

        // Subscription services
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // AutoMapper - ✅ ADD RoleProfile
        services.AddAutoMapper(_ => { }, 
            typeof(UserProfile), 
            typeof(ClinicProfile), 
            typeof(PermissionProfile), 
            typeof(SubscriptionProfile),
            typeof(RoleProfile));  // ✅ ADD THIS

        // FluentValidation - ✅ This will automatically find Role validators
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // Seeder registrations
        services.AddScoped<DbSeeder>();
        services.AddScoped<ISingleSeeder, RolesSeeder>();           // Order 1 - Create roles first
        services.AddScoped<ISingleSeeder, PermissionsSeeder>();     // Order 2 - Create permissions
        services.AddScoped<ISingleSeeder, RolePermissionsSeeder>(); // Order 3 - Assign permissions to roles
        services.AddScoped<ISingleSeeder, SuperAdminSeeder>();      // Order 4 - Create SuperAdmin user

        return services;
    }
}