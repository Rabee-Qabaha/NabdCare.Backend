using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.IntegrationTests.TestFixtures;

/// <summary>
/// Comprehensive test factory with full permission testing support.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 21:12:45 UTC
/// </summary>
public class NabdCareWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    public Guid SuperAdminRoleId { get; private set; }
    public Guid ClinicAdminRoleId { get; private set; }
    public Guid DoctorRoleId { get; private set; }
    public Guid NurseRoleId { get; private set; }
    public Guid ClinicId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Key", "ThisIsAVerySecureTestKeyWith32CharactersMinimum!");
        builder.UseSetting("Jwt:Issuer", "NabdCareTestIssuer");
        builder.UseSetting("Jwt:Audience", "NabdCareTestAudience");
        builder.UseSetting("Jwt:ExpireMinutes", "60");

        builder.ConfigureServices(services =>
        {
            // Remove production DbContext
            services.RemoveAll(typeof(DbContextOptions<NabdCareDbContext>));
            services.RemoveAll(typeof(NabdCareDbContext));

            // ✅ Remove Rate Limiting services properly
            var rateLimitingServices = services
                .Where(d => d.ServiceType.FullName != null && 
                            (d.ServiceType.FullName.Contains("RateLimiter") ||
                             d.ServiceType.FullName.Contains("RateLimiting")))
                .ToList();
        
            foreach (var service in rateLimitingServices)
            {
                services.Remove(service);
            }
        
            // Remove ALL hosted services
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // ✅ Use In-Memory SQLite
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<NabdCareDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NabdCareDbContext>();
        
        await dbContext.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(dbContext);
    }

    private async Task SeedTestDataAsync(NabdCareDbContext dbContext)
    {
        var roles = CreateRoles();
        dbContext.Roles.AddRange(roles);
        await dbContext.SaveChangesAsync();

        SuperAdminRoleId = roles.First(r => r.Name == "SuperAdmin").Id;
        ClinicAdminRoleId = roles.First(r => r.Name == "Clinic Admin").Id;
        DoctorRoleId = roles.First(r => r.Name == "Doctor").Id;
        NurseRoleId = roles.First(r => r.Name == "Nurse").Id;

        var permissions = CreatePermissions();
        dbContext.AppPermissions.AddRange(permissions);
        await dbContext.SaveChangesAsync();

        var rolePermissions = CreateRolePermissions(roles, permissions);
        dbContext.RolePermissions.AddRange(rolePermissions);
        await dbContext.SaveChangesAsync();

        var clinic = CreateClinic();
        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();

        ClinicId = clinic.Id;

        var users = CreateUsers(roles, clinic.Id);
        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"✅ Test data seeded:");
        Console.WriteLine($"   - Roles: {roles.Count}");
        Console.WriteLine($"   - Permissions: {permissions.Count}");
        Console.WriteLine($"   - Role-Permissions: {rolePermissions.Count}");
        Console.WriteLine($"   - Users: {users.Count}");
    }

    private List<Role> CreateRoles()
    {
        return new List<Role>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "SuperAdmin",
                Description = "Super Administrator with full system access",
                IsSystemRole = true,
                IsTemplate = false,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Clinic Admin",
                Description = "Clinic Administrator",
                IsSystemRole = false,
                IsTemplate = true,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Doctor",
                Description = "Medical Doctor",
                IsSystemRole = false,
                IsTemplate = true,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Nurse",
                Description = "Nurse",
                IsSystemRole = false,
                IsTemplate = true,
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Receptionist",
                Description = "Front Desk Receptionist",
                IsSystemRole = false,
                IsTemplate = true,
                DisplayOrder = 5,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            }
        };
    }

    private List<AppPermission> CreatePermissions()
    {
        var permissions = new List<AppPermission>();
        var categories = new[]
        {
            ("Users", new[] { "View", "Create", "Edit", "Delete", "Manage", "Activate", "ResetPassword", "ChangeRole", "ManagePermissions" }),
            ("Roles", new[] { "View", "Create", "Edit", "Delete", "Manage" }),
            // ✅ ADD: Assign and Revoke permissions
            ("Permissions", new[] { "View", "Assign", "Revoke" }),
            ("Settings", new[] { "View", "Edit", "ManageRoles" }),
            ("Clinics", new[] { "View", "Create", "Edit", "Delete", "Manage", "ManageStatus", "ViewStats", "ViewAll", "HardDelete" }),
            ("Patients", new[] { "View", "Create", "Edit", "Delete" }),
            ("Appointments", new[] { "View", "Create", "Edit", "Delete", "Cancel" }),
            ("MedicalRecords", new[] { "View", "Create", "Edit" }),
            ("Billing", new[] { "View", "Create", "Edit", "Delete" }),
            ("Reports", new[] { "View", "Generate" }),
            ("System", new[] { "ManageSettings", "ManageRoles" }),
            ("Subscriptions", new[] { "View", "ViewAll", "Create", "Edit", "Cancel" })
        };

        foreach (var (category, actions) in categories)
        {
            foreach (var action in actions)
            {
                permissions.Add(new AppPermission
                {
                    Id = Guid.NewGuid(),
                    Name = $"{category}.{action}",
                    Description = $"{action} {category}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Rabee-Qabaha"
                });
            }
        }

        return permissions;
    }

private List<RolePermission> CreateRolePermissions(List<Role> roles, List<AppPermission> permissions)
{
    var rolePermissions = new List<RolePermission>();
    var superAdminRole = roles.First(r => r.Name == "SuperAdmin");
    var clinicAdminRole = roles.First(r => r.Name == "Clinic Admin");
    var doctorRole = roles.First(r => r.Name == "Doctor");

    // SuperAdmin gets ALL permissions
    foreach (var permission in permissions)
    {
        rolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = superAdminRole.Id,
            PermissionId = permission.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Rabee-Qabaha"
        });
    }

    // ✅ UPDATED: Clinic Admin gets role management permissions
    var clinicAdminPermissions = permissions.Where(p =>
        // Users permissions
        p.Name.StartsWith("Users.") ||
        // Role management permissions ← ADDED
        p.Name.StartsWith("Roles.") ||
        p.Name == "Settings.ManageRoles" ||
        // Permissions viewing
        p.Name.StartsWith("Permissions.View") ||
        // Other permissions (excluding system-level)
        (!p.Name.StartsWith("Clinics.Delete") && 
         !p.Name.StartsWith("Clinics.Create") &&
         !p.Name.StartsWith("System."))).ToList();

    foreach (var permission in clinicAdminPermissions)
    {
        rolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = clinicAdminRole.Id,
            PermissionId = permission.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Rabee-Qabaha"
        });
    }

    // Doctor gets limited permissions (no role/user management)
    var doctorPermissions = permissions.Where(p =>
        p.Name.StartsWith("Patients.") ||
        p.Name.StartsWith("Appointments.") ||
        p.Name.StartsWith("MedicalRecords.")).ToList();

    foreach (var permission in doctorPermissions)
    {
        rolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = doctorRole.Id,
            PermissionId = permission.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Rabee-Qabaha"
        });
    }

    return rolePermissions;
}

    private Clinic CreateClinic()
    {
        var clinicId = Guid.NewGuid();
        return new Clinic
        {
            Id = clinicId,
            Name = "Test Medical Center",
            Email = "test@clinic.com",
            Phone = "+1234567890",
            Address = "123 Test Street, Test City",
            Status = SubscriptionStatus.Active,
            BranchCount = 2,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Rabee-Qabaha",
            Subscriptions = new List<Subscription>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddYears(1),
                    Type = SubscriptionType.Yearly,
                    Fee = 12000m,
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Rabee-Qabaha"
                }
            }
        };
    }

    private List<User> CreateUsers(List<Role> roles, Guid clinicId)
    {
        const string passwordHash = "AQAAAAIAAYagAAAAEKbMdwJR3BaUX8+jsjp+UOJNOp0+UA7wFD5uyOfrc2t2C8TKlWphyn5gFn2eGnRm9w==";

        return new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "sadmin@nabd.care",
                FullName = "Super Administrator",
                PasswordHash = passwordHash,
                RoleId = roles.First(r => r.Name == "SuperAdmin").Id,
                ClinicId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "cadmin@nabd.care",
                FullName = "Clinic Administrator",
                PasswordHash = passwordHash,
                RoleId = roles.First(r => r.Name == "Clinic Admin").Id,
                ClinicId = clinicId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "dadmin@nabd.care",
                FullName = "Dr. Test Doctor",
                PasswordHash = passwordHash,
                RoleId = roles.First(r => r.Name == "Doctor").Id,
                ClinicId = clinicId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "nurse@nabd.care",
                FullName = "Test Nurse",
                PasswordHash = passwordHash,
                RoleId = roles.First(r => r.Name == "Nurse").Id,
                ClinicId = clinicId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "receptionist@nabd.care",
                FullName = "Test Receptionist",
                PasswordHash = passwordHash,
                RoleId = roles.First(r => r.Name == "Receptionist").Id,
                ClinicId = clinicId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Rabee-Qabaha"
            }
        };
    }

    public new async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}