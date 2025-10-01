using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Application.mappings;
using NabdCare.Application.Mappings;
using NabdCare.Application.Services;
using NabdCare.Application.Services.Auth;
using NabdCare.Application.Services.Clinics;
using NabdCare.Application.Services.Permissions;
using NabdCare.Application.Services.Users;
using NabdCare.Application.Validator.Clinics.Subscriptions;
using NabdCare.Application.Validator.clinics;
using NabdCare.Application.Validator.Users;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Auth;
using NabdCare.Infrastructure.Repositories.Clinics;
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

        // Clinic services
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicService, ClinicService>();

        // Subscription services
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // AutoMapper
        services.AddAutoMapper(_ => { }, typeof(UserProfile), typeof(ClinicProfile), typeof(PermissionProfile), typeof(SubscriptionProfile));
        
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // Seeder
        services.AddScoped<DbSeeder>();
        services.AddScoped<ISingleSeeder, SuperAdminSeeder>();
        services.AddScoped<ISingleSeeder, PermissionSeeder>();
        services.AddHostedService<DbSeedHostedService>();

        return services;
    }
}