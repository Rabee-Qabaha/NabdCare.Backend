using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Api.Extensions;

namespace NabdCare.Api.Endpoints;

public static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/permissions").WithTags("Permissions");

        // GET all
        group.MapGet("/", async ([FromServices] IPermissionService service) =>
        {
            var result = await service.GetAllPermissionsAsync();
            return Results.Ok(result);
        })
        .RequirePermission("ViewUsers")
        .WithSummary("Get all permissions");

        // GET by ID
        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IPermissionService service) =>
        {
            var permission = await service.GetPermissionByIdAsync(id);
            return permission != null ? Results.Ok(permission) : Results.NotFound();
        })
        .RequirePermission("ViewUsers");

        // POST create
        group.MapPost("/", async ([FromBody] CreatePermissionDto dto, [FromServices] IPermissionService service) =>
        {
            var created = await service.CreatePermissionAsync(dto);
            return Results.Created($"/api/permissions/{created.Id}", created);
        })
        .RequirePermission("CreateUser"); // Example permission

        // PUT update
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdatePermissionDto dto, [FromServices] IPermissionService service) =>
        {
            var updated = await service.UpdatePermissionAsync(id, dto);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequirePermission("UpdateUser");

        // DELETE
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IPermissionService service) =>
        {
            var deleted = await service.DeletePermissionAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequirePermission("DeleteUser");

        // POST assign to role
        group.MapPost("/assign-role", async ([FromBody] AssignPermissionDto dto, [FromServices] IPermissionService service) =>
        {
            var assigned = await service.AssignPermissionToRoleAsync(dto.Role, dto.PermissionId);
            return assigned ? Results.Ok() : Results.BadRequest();
        })
        .RequirePermission("UpdateUserRole");

        // POST assign to user
        group.MapPost("/assign-user", async ([FromBody] AssignPermissionToUserDto dto, [FromServices] IPermissionService service) =>
        {
            var assigned = await service.AssignPermissionToUserAsync(dto.UserId, dto.PermissionId);
            return assigned ? Results.Ok() : Results.BadRequest();
        })
        .RequirePermission("UpdateUserRole");
    }
}