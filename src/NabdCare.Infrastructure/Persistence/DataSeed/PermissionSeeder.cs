using NabdCare.Application.Common;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class PermissionSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;

    public PermissionSeeder(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        // 1. Define all explicit permissions
        var permissions = new List<AppPermission>
        {
            new() { Name = "CreateUser", Description = "Create a new user" },
            new() { Name = "ViewUser", Description = "View a single user" },
            new() { Name = "ViewUsers", Description = "View all users" },
            new() { Name = "UpdateUser", Description = "Update user details" },
            new() { Name = "UpdateUserRole", Description = "Change user's role" },
            new() { Name = "DeleteUser", Description = "Soft delete a user" }
        };

        foreach (var perm in permissions)
        {
            var exists = await _dbContext.AppPermission
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Name == perm.Name);

            if (!exists)
            {
                await _dbContext.AppPermission.AddAsync(perm);
            }
        }

        await _dbContext.SaveChangesAsync();

        // 2. Assign permissions to roles
        var rolePermissions = new Dictionary<UserRole, string[]>
        {
            { UserRole.SuperAdmin, permissions.Select(p => p.Name).ToArray() }, 
            { UserRole.ClinicAdmin, new[] { "CreateUser", "ViewUser", "ViewUsers", "UpdateUser" } },
            { UserRole.Doctor, Array.Empty<string>() },
            { UserRole.Nurse, Array.Empty<string>() },
            { UserRole.Receptionist, Array.Empty<string>() }
        };

        foreach (var kv in rolePermissions)
        {
            var role = kv.Key;
            foreach (var permName in kv.Value)
            {
                var permission = await _dbContext.AppPermission
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Name == permName);

                if (permission == null) continue;

                var existsRp = await _dbContext.RolePermissions
                    .IgnoreQueryFilters()
                    .AnyAsync(rp => rp.Role == role && rp.PermissionId == permission.Id);

                if (!existsRp)
                {
                    await _dbContext.RolePermissions.AddAsync(new RolePermission
                    {
                        Role = role,
                        PermissionId = permission.Id
                    });
                }
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}