using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
            // Soft cap for total cache size (in arbitrary “size units”)
            // This prevents memory overuse in high concurrency environments.
            options.SizeLimit = 10_000; // You can tune this (e.g., 50_000 for big servers)

            // Optional: set overall compaction percentage
            options.CompactionPercentage = 0.2; // Remove 20% of least used entries when limit hit

            // Optional: add a name for logging/tracing
            options.TrackStatistics = true; // Requires .NET 8+, gives you cache metrics
        });

        // Cache invalidator (used in RoleRepository & PermissionRepository)
        services.AddScoped<IPermissionCacheInvalidator, PermissionCacheInvalidator>();

        // Permission service with caching decorator
        services.AddScoped<PermissionService>();
        services.AddScoped<IPermissionService, CachedPermissionService>();

        // Permission evaluator (combines RBAC + PBAC + ABAC)
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();

        // ABAC Policies
        services.AddScoped<IAccessPolicy<NabdCare.Domain.Entities.Patients.Patient>, PatientPolicy>();
        services.AddScoped<IAccessPolicy<NabdCare.Domain.Entities.Clinics.Clinic>, ClinicPolicy>();

        // RBAC Authorization Provider
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
        services.AddAutoMapper(cfg => { },
            typeof(UserProfile).Assembly,
            typeof(ClinicProfile).Assembly,
            typeof(PermissionProfile).Assembly,
            typeof(SubscriptionProfile).Assembly,
            typeof(RoleProfile).Assembly
        );

        // ===============================
        // Validation
        // ===============================
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // ===============================
        // Database Seeders
        // ===============================
        services.AddScoped<DbSeeder>();
        services.AddScoped<ISingleSeeder, RolesSeeder>();
        services.AddScoped<ISingleSeeder, PermissionsSeeder>();
        services.AddScoped<ISingleSeeder, RolePermissionsSeeder>();
        services.AddScoped<ISingleSeeder, SuperAdminSeeder>();

        return services;
    }
}