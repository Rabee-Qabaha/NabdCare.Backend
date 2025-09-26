using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Permission;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.EndPoints;

    public static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/permissions")
            .WithTags("Permissions");

        // GET all permissions
        group.MapGet("/", async (IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var permissions = await permissionService.GetAllPermissionsAsync();
                return Results.Ok(permissions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching permissions");
                return Results.Problem("An error occurred while fetching permissions");
            }
        });

        // GET permission by Id
        group.MapGet("/{id:guid}", async (Guid id, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var permission = await permissionService.GetPermissionByIdAsync(id);
                return permission is not null ? Results.Ok(permission) : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching permission {PermissionId}", id);
                return Results.Problem("An error occurred while fetching the permission");
            }
        });

        // POST: create permission
        group.MapPost("/", async (Permission permission, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var created = await permissionService.CreatePermissionAsync(permission);
                return Results.Created($"/permissions/{created.Id}", created);
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "DB error creating permission");
                return Results.Problem("Database error while creating permission");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating permission");
                return Results.Problem("An error occurred while creating permission");
            }
        });

        // PUT: update permission
        group.MapPut("/{id:guid}", async (Guid id, Permission permission, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var updated = await permissionService.UpdatePermissionAsync(id, permission);
                return updated is not null ? Results.Ok(updated) : Results.NotFound();
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "DB error updating permission {PermissionId}", id);
                return Results.Problem("Database error while updating permission");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating permission {PermissionId}", id);
                return Results.Problem("An error occurred while updating permission");
            }
        });

        // DELETE: delete permission
        group.MapDelete("/{id:guid}", async (Guid id, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var deleted = await permissionService.DeletePermissionAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "DB error deleting permission {PermissionId}", id);
                return Results.Problem("Database error while deleting permission");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting permission {PermissionId}", id);
                return Results.Problem("An error occurred while deleting permission");
            }
        });

        // POST: assign permission to role
        group.MapPost("/assign-role", async (UserRole role, Guid permissionId, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var assigned = await permissionService.AssignPermissionToRoleAsync(role, permissionId);
                return assigned ? Results.Ok() : Results.BadRequest("Permission already assigned or invalid");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning permission {PermissionId} to role {Role}", permissionId, role);
                return Results.Problem("An error occurred while assigning permission to role");
            }
        });

        // POST: assign permission to user
        group.MapPost("/assign-user", async (Guid userId, Guid permissionId, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var assigned = await permissionService.AssignPermissionToUserAsync(userId, permissionId);
                return assigned ? Results.Ok() : Results.BadRequest("Permission already assigned or invalid");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning permission {PermissionId} to user {UserId}", permissionId, userId);
                return Results.Problem("An error occurred while assigning permission to user");
            }
        });
    }
}
