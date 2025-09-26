using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Permissions;
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

        // GET appPermission by Id
        group.MapGet("/{id:guid}", async (Guid id, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var permission = await permissionService.GetPermissionByIdAsync(id);
                return permission is not null ? Results.Ok(permission) : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching appPermission {PermissionId}", id);
                return Results.Problem("An error occurred while fetching the appPermission");
            }
        });

        // POST: create appPermission
        group.MapPost("/", async (AppPermission appPermission, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var created = await permissionService.CreatePermissionAsync(appPermission);
                return Results.Created($"/permissions/{created.Id}", created);
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "DB error creating appPermission");
                return Results.Problem("Database error while creating appPermission");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating appPermission");
                return Results.Problem("An error occurred while creating appPermission");
            }
        });

        // PUT: update appPermission
        group.MapPut("/{id:guid}", async (Guid id, AppPermission appPermission, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var updated = await permissionService.UpdatePermissionAsync(id, appPermission);
                return updated is not null ? Results.Ok(updated) : Results.NotFound();
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "DB error updating appPermission {PermissionId}", id);
                return Results.Problem("Database error while updating appPermission");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating appPermission {PermissionId}", id);
                return Results.Problem("An error occurred while updating appPermission");
            }
        });

        // DELETE: delete appPermission
        group.MapDelete("/{id:guid}", async (Guid id, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var deleted = await permissionService.DeletePermissionAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "DB error deleting appPermission {PermissionId}", id);
                return Results.Problem("Database error while deleting appPermission");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting appPermission {PermissionId}", id);
                return Results.Problem("An error occurred while deleting appPermission");
            }
        });

        // POST: assign appPermission to role
        group.MapPost("/assign-role", async (UserRole role, Guid permissionId, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var assigned = await permissionService.AssignPermissionToRoleAsync(role, permissionId);
                return assigned ? Results.Ok() : Results.BadRequest("AppPermission already assigned or invalid");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning appPermission {PermissionId} to role {Role}", permissionId, role);
                return Results.Problem("An error occurred while assigning appPermission to role");
            }
        });

        // POST: assign appPermission to user
        group.MapPost("/assign-user", async (Guid userId, Guid permissionId, IPermissionService permissionService, ILogger<Program> logger) =>
        {
            try
            {
                var assigned = await permissionService.AssignPermissionToUserAsync(userId, permissionId);
                return assigned ? Results.Ok() : Results.BadRequest("AppPermission already assigned or invalid");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning appPermission {PermissionId} to user {UserId}", permissionId, userId);
                return Results.Problem("An error occurred while assigning appPermission to user");
            }
        });
    }
}
