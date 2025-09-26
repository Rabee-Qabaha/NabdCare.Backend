using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

public static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/permissions").WithTags("Permissions");

        // Get all permissions
        group.MapGet("/", async (
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var result = await service.GetAllPermissionsAsync();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching permissions");
                return Results.Problem("An error occurred while fetching permissions.");
            }
        })
        .WithName("GetAllPermissions")
        .WithOpenApi();

        // Get by Id
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var permission = await service.GetPermissionByIdAsync(id);
                return permission is not null ? Results.Ok(permission) : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching permission {PermissionId}", id);
                return Results.Problem("An error occurred while fetching the permission.");
            }
        })
        .WithName("GetPermissionById")
        .WithOpenApi();

        // Create
        group.MapPost("/", async (
            [FromBody] AppPermission permission,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(permission.Name))
                    return Results.BadRequest("Permission name is required.");

                var created = await service.CreatePermissionAsync(permission);
                return Results.Created($"/api/permissions/{created.Id}", created);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating permission");
                return Results.Problem("An error occurred while creating the permission.");
            }
        })
        .WithName("CreatePermission")
        .WithOpenApi();

        // Update
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] AppPermission permission,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var updated = await service.UpdatePermissionAsync(id, permission);
                return updated is not null ? Results.Ok(updated) : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating permission {PermissionId}", id);
                return Results.Problem("An error occurred while updating the permission.");
            }
        })
        .WithName("UpdatePermission")
        .WithOpenApi();

        // Delete
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var deleted = await service.DeletePermissionAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting permission {PermissionId}", id);
                return Results.Problem("An error occurred while deleting the permission.");
            }
        })
        .WithName("DeletePermission")
        .WithOpenApi();

        // Assign to role
        group.MapPost("/assign-role", async (
            [FromQuery] UserRole role,
            [FromQuery] Guid permissionId,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var assigned = await service.AssignPermissionToRoleAsync(role, permissionId);
                return assigned ? Results.Ok() : Results.BadRequest("Permission already assigned or invalid.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning permission {PermissionId} to role {Role}", permissionId, role);
                return Results.Problem("An error occurred while assigning permission to role.");
            }
        })
        .WithName("AssignPermissionToRole")
        .WithOpenApi();

        // Remove from role
        group.MapDelete("/remove-role", async (
            [FromQuery] UserRole role,
            [FromQuery] Guid permissionId,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var removed = await service.RemovePermissionFromRoleAsync(role, permissionId);
                return removed ? Results.Ok() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing permission {PermissionId} from role {Role}", permissionId, role);
                return Results.Problem("An error occurred while removing permission from role.");
            }
        })
        .WithName("RemovePermissionFromRole")
        .WithOpenApi();

        // Assign to user
        group.MapPost("/assign-user", async (
            [FromQuery] Guid userId,
            [FromQuery] Guid permissionId,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var assigned = await service.AssignPermissionToUserAsync(userId, permissionId);
                return assigned ? Results.Ok() : Results.BadRequest("Permission already assigned or invalid.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning permission {PermissionId} to user {UserId}", permissionId, userId);
                return Results.Problem("An error occurred while assigning permission to user.");
            }
        })
        .WithName("AssignPermissionToUser")
        .WithOpenApi();

        // Remove from user
        group.MapDelete("/remove-user", async (
            [FromQuery] Guid userId,
            [FromQuery] Guid permissionId,
            [FromServices] IPermissionService service,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var removed = await service.RemovePermissionFromUserAsync(userId, permissionId);
                return removed ? Results.Ok() : Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing permission {PermissionId} from user {UserId}", permissionId, userId);
                return Results.Problem("An error occurred while removing permission from user.");
            }
        })
        .WithName("RemovePermissionFromUser")
        .WithOpenApi();
    }
}