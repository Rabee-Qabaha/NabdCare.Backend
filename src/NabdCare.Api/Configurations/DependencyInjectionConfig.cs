using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Audit;
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
using NabdCare.Application.Services.Permissions.Policies;
using NabdCare.Application.Services.Roles;
using NabdCare.Application.Services.Users;
using NabdCare.Application.Validator.Users;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Audit;
using NabdCare.Infrastructure.Repositories.Auth;
using NabdCare.Infrastructure.Repositories.Clinics;
using NabdCare.Infrastructure.Repositories.Permissions;
using NabdCare.Infrastructure.Repositories.Roles;
using NabdCare.Infrastructure.Repositories.Users;
using NabdCare.Api.Authorization;
using NabdCare.Application.mappings;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Infrastructure.Services.Caching;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ===============================
        // Database
        // ===============================
        services.AddDbContext<NabdCareDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // ===============================
        // Contexts
        // ===============================
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUserContext, UserContext>();

        // ===============================
        // Authentication & Authorization
        // ===============================
        services.AddScoped<IPasswordService, IdentityPasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // ===============================
        // Core Modules
        // ===============================
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleService, RoleService>();

        services.AddScoped<IPermissionRepository, PermissionRepository>();

        // ===============================
        // Permission Caching & Evaluation
        // ===============================
        services.AddMemoryCache(options =>
        {
            // ✅ Prevent excessive memory usage under heavy load
            options.SizeLimit = 10_000;

            // ✅ Compact 20% of least-used entries when limit is hit
            options.CompactionPercentage = 0.2;

            // ✅ Enable cache metrics (requires .NET 8+)
            options.TrackStatistics = true;
        });

        // Used by repositories to invalidate permission-related cache entries
        services.AddScoped<IPermissionCacheInvalidator, PermissionCacheInvalidator>();

        // ---------------------------------------------------------------
        // 🧠 Caching Decorator Pattern
        // ---------------------------------------------------------------
        // PermissionService = core business logic (no caching)
        // CachedPermissionService = wraps PermissionService, adds caching, version tracking
        // IPermissionService resolves to CachedPermissionService by default
        // ---------------------------------------------------------------
        services.AddScoped<PermissionService>();
        services.AddScoped<CachedPermissionService>();
        services.AddScoped<IPermissionService, CachedPermissionService>();

        // Evaluates access dynamically via RBAC + PBAC + ABAC
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();

        // ===============================
        // ABAC Policies
        // ===============================
        services.AddScoped<IAccessPolicy<Clinic>, ClinicPolicy>();
        // services.AddScoped<IAccessPolicy<Patient>, PatientPolicy>();
        services.AddScoped<IAccessPolicy<User>, UserPolicy>();
        services.AddScoped<IAccessPolicy<Role>, RolePolicy>();
        services.AddScoped<IAccessPolicy<AppPermission>, PermissionPolicy>();
        services.AddScoped<IAccessPolicy<Subscription>, SubscriptionPolicy>();

        // Default fallback for unmapped entities
        services.AddScoped(typeof(IAccessPolicy<>), typeof(DefaultPolicy<>));
        
        // ===============================
        // RBAC Authorization Provider
        // ===============================
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ===============================
        // Clinics & Subscriptions
        // ===============================
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicService, ClinicService>();

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // ===============================
        // Audit
        // ===============================
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        
        // ===============================
        // Automapper
        // ===============================
        services.AddAutoMapper(_ => { },
            typeof(UserProfile).Assembly,
            typeof(ClinicProfile).Assembly,
            typeof(PermissionProfile).Assembly,
            typeof(SubscriptionProfile).Assembly,
            typeof(RoleProfile).Assembly,
            typeof(AuditLogMappingProfile).Assembly
        );

        // ===============================
        // Validation
        // ===============================
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // ===============================
        // Database Seeders
        // ===============================
        services.AddScoped<DbSeeder>();
        services.AddScoped<ISingleSeeder, RoleSeeder>();
        services.AddScoped<ISingleSeeder, PermissionsSeeder>();
        services.AddScoped<ISingleSeeder, RolePermissionsSeeder>();
        services.AddScoped<ISingleSeeder, SuperAdminSeeder>();

        return services;
    }
}