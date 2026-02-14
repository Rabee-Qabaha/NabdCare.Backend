using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Audit;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Application.Interfaces.Configuration;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Application.Interfaces.Invoices;
using NabdCare.Application.Interfaces.Payments;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Reports;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Application.Mappings;
using NabdCare.Application.Services;
using NabdCare.Application.Services.Auth;
using NabdCare.Application.Services.Clinics;
using NabdCare.Application.Services.Configuration;
using NabdCare.Application.Services.Invoices;
using NabdCare.Application.Services.Payments;
using NabdCare.Application.Services.Permissions;
using NabdCare.Application.Services.Permissions.Policies;
using NabdCare.Application.Services.Reports;
using NabdCare.Application.Services.Roles;
using NabdCare.Application.Services.Users;
using NabdCare.Application.Validator.Users;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Persistence.DataSeed;
using NabdCare.Infrastructure.Repositories.Audit;
using NabdCare.Infrastructure.Repositories.Auth;
using NabdCare.Infrastructure.Repositories.Clinics;
using NabdCare.Infrastructure.Repositories.Configuration;
using NabdCare.Infrastructure.Repositories.Invoices;
using NabdCare.Infrastructure.Repositories.Payments;
using NabdCare.Infrastructure.Repositories.Reports;
using NabdCare.Infrastructure.Repositories.Permissions;
using NabdCare.Infrastructure.Repositories.Roles;
using NabdCare.Infrastructure.Repositories.Users;
using NabdCare.Api.Authorization;
using NabdCare.Application.Interfaces.Authorizations;
using NabdCare.Application.mappings;
using NabdCare.Application.Services.Authorizations;
using NabdCare.Application.Services.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Infrastructure.BackgroundJobs;
using NabdCare.Infrastructure.Repositories.Authorization;
using NabdCare.Infrastructure.Repositories.Subscriptions;
using NabdCare.Infrastructure.Services.Caching;

namespace NabdCare.Api.Configurations;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddNabdCareServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ===============================
        // HTTP Client Factory
        // ===============================
        services.AddHttpClient();

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
            options.SizeLimit = 10_000;
            options.CompactionPercentage = 0.2;
            options.TrackStatistics = true;
        });

        services.AddScoped<IPermissionCacheInvalidator, PermissionCacheInvalidator>();

        // Decorator Pattern for Caching
        services.AddScoped<PermissionService>();
        services.AddScoped<CachedPermissionService>();
        services.AddScoped<IPermissionService, CachedPermissionService>();

        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();

        // ===============================
        // ABAC Policies
        // ===============================
        services.AddScoped<IAccessPolicy<Clinic>, ClinicPolicy>();
        services.AddScoped<IAccessPolicy<Branch>, BranchPolicy>();
        services.AddScoped<IAccessPolicy<User>, UserPolicy>();
        services.AddScoped<IAccessPolicy<Role>, RolePolicy>();
        services.AddScoped<IAccessPolicy<AppPermission>, PermissionPolicy>();
        services.AddScoped<IAccessPolicy<Subscription>, SubscriptionPolicy>();
        services.AddScoped<IAccessPolicy<Invoice>, InvoicePolicy>();
        services.AddScoped<IAccessPolicy<Payment>, PaymentPolicy>();
        services.AddScoped(typeof(IAccessPolicy<>), typeof(DefaultPolicy<>));
        
        // ===============================
        // RBAC Authorization Provider
        // ===============================
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ===============================
        // Clinics, Branches & Subscriptions
        // ===============================
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicService, ClinicService>();

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBranchService, BranchService>();

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        services.AddScoped<IClinicDashboardRepository, ClinicDashboardRepository>();
        services.AddScoped<IClinicDashboardService, ClinicDashboardService>();
        
        // ===============================
        // Invoices (Billing)
        // ===============================
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceService, InvoiceService>();

        // ===============================
        // Payments
        // ===============================
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        // ===============================
        // Reports
        // ===============================
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportService, ReportService>();

        // ===============================
        // Configuration
        // ===============================
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        // ===============================
        // Authorization (Audit/Access)
        // ===============================
        services.AddScoped<IAuthorizationRepository, AuthorizationRepository>();
        services.AddScoped<Application.Interfaces.Authorizations.IAuthorizationService, AuthorizationService>();
        
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
            typeof(BranchProfile).Assembly,
            typeof(PermissionProfile).Assembly,
            typeof(SubscriptionProfile).Assembly,
            typeof(RoleProfile).Assembly,
            typeof(InvoiceProfile).Assembly,
            typeof(PaymentProfile).Assembly,
            typeof(AuditLogMappingProfile).Assembly
        );

        // ===============================
        // Validation
        // ===============================
        // Scans assembly for all AbstractValidators
        services.AddValidatorsFromAssemblyContaining<UserValidator>();

        // ===============================
        // Database Seeders
        // ===============================
        services.AddScoped<DbSeeder>();
        services.AddScoped<ISingleSeeder, RoleSeeder>();
        services.AddScoped<ISingleSeeder, PermissionsSeeder>();
        services.AddScoped<ISingleSeeder, RolePermissionsSeeder>();
        services.AddScoped<ISingleSeeder, SuperAdminSeeder>();

        // ===============================
        // Background Jobs
        // ===============================
        services.AddScoped<SubscriptionLifecycleJob>();
        services.AddScoped<InvoiceOverdueJob>();
        services.AddScoped<ExchangeRateFetcherJob>();

        if (configuration["ASPNETCORE_ENVIRONMENT"] != "Testing")
        {
            services.AddHostedService<SubscriptionScheduler>();
            services.AddHostedService<ExchangeRateScheduler>();
        }
        
        return services;
    }
}