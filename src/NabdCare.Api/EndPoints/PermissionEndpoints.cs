using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

public static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/permissions").WithTags("Permissions");

        // Get all permissions
        group.MapGet("/", async ([FromServices] IPermissionService service) =>
        {
            var result = await service.GetAllPermissionsAsync();
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithSummary("Get all permissions")
        .WithOpenApi();

        // Get permission by ID
        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IPermissionService service) =>
        {
            var permission = await service.GetPermissionByIdAsync(id);
            return permission is not null ? Results.Ok(permission) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Get permission by ID")
        .WithOpenApi();

        // Create permission
        group.MapPost("/", async ([FromBody] AppPermission permission, [FromServices] IPermissionService service) =>
        {
            if (string.IsNullOrWhiteSpace(permission.Name))
                return Results.BadRequest("Permission name is required.");

            var created = await service.CreatePermissionAsync(permission);
            return Results.Created($"/api/permissions/{created.Id}", created);
        })
        .RequireAuthorization("SuperAdmin")
        .WithSummary("Create a new permission")
        .WithOpenApi();

        // Update permission
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] AppPermission permission, [FromServices] IPermissionService service) =>
        {
            var updated = await service.UpdatePermissionAsync(id, permission);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization("SuperAdmin")
        .WithSummary("Update a permission")
        .WithOpenApi();

        // Delete permission
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IPermissionService service) =>
        {
            var deleted = await service.DeletePermissionAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("SuperAdmin")
        .WithSummary("Delete a permission")
        .WithOpenApi();

        // Assign permission to role
        group.MapPost("/assign-role", async ([FromQuery] UserRole role, [FromQuery] Guid permissionId, [FromServices] IPermissionService service) =>
        {
            var assigned = await service.AssignPermissionToRoleAsync(role, permissionId);
            return assigned ? Results.Ok() : Results.BadRequest("Permission already assigned or invalid.");
        })
        .RequireAuthorization("SuperAdmin")
        .WithSummary("Assign permission to role")
        .WithOpenApi();

        // Remove permission from role
        group.MapDelete("/remove-role", async ([FromQuery] UserRole role, [FromQuery] Guid permissionId, [FromServices] IPermissionService service) =>
        {
            var removed = await service.RemovePermissionFromRoleAsync(role, permissionId);
            return removed ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization("SuperAdmin")
        .WithSummary("Remove permission from role")
        .WithOpenApi();

        // Assign permission to user
        group.MapPost("/assign-user", async ([FromQuery] Guid userId, [FromQuery] Guid permissionId, [FromServices] IPermissionService service) =>
        {
            var assigned = await service.AssignPermissionToUserAsync(userId, permissionId);
            return assigned ? Results.Ok() : Results.BadRequest("Permission already assigned or invalid.");
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithSummary("Assign permission to user")
        .WithOpenApi();

        // Remove permission from user
        group.MapDelete("/remove-user", async ([FromQuery] Guid userId, [FromQuery] Guid permissionId, [FromServices] IPermissionService service) =>
        {
            var removed = await service.RemovePermissionFromUserAsync(userId, permissionId);
            return removed ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithSummary("Remove permission from user")
        .WithOpenApi();
    }
}