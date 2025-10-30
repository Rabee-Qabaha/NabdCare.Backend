using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Api.Extensions;
using NabdCare.Application.DTOs.Pagination;

namespace NabdCare.Api.Endpoints;

public static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/permissions").WithTags("Permissions");

// ============================================
        // 📄 GET ALL PERMISSIONS (PAGINATED)
        // ============================================
        group.MapGet("/paged", async (
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] IPermissionService service) =>
        {
            try
            {
                var result = await service.GetAllPagedAsync(pagination);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to retrieve paged permissions");
            }
        })
        .RequirePermission("Permissions.View")
        .Produces<PaginatedResult<PermissionResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithSummary("Get all permissions (paginated, supports filter/sort/cursor)");

        // ============================================
        // 📋 GET ALL PERMISSIONS (LEGACY - NON PAGED)
        // ============================================
        group.MapGet("/", async (
            [FromServices] IPermissionService service) =>
        {
            try
            {
                var result = await service.GetAllPermissionsAsync();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to retrieve all permissions");
            }
        })
        .RequirePermission("Permissions.View")
        .Produces<IEnumerable<PermissionResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithSummary("Get all permissions (non-paginated)");

        // ============================================
        // 📊 GET PERMISSIONS GROUPED BY CATEGORY
        // ============================================
        group.MapGet("/grouped", async (
            [FromServices] IPermissionService service) =>
        {
            try
            {
                var permissions = await service.GetAllPermissionsAsync();

                var grouped = permissions
                    .GroupBy(p => p.Name.Split('.')[0]) // e.g. "Users.Create" → "Users"
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Permissions = g.OrderBy(p => p.Name).ToList()
                    });

                return Results.Ok(grouped);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to retrieve grouped permissions");
            }
        })
        .RequirePermission("Permissions.View")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithSummary("Get permissions grouped by category (Users, Patients, etc.)");

        // ============================================
        // 🔍 GET PERMISSION BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IPermissionService service) =>
        {
            var permission = await service.GetPermissionByIdAsync(id);
            return permission != null ? Results.Ok(permission) : Results.NotFound();
        })
        .RequirePermission("Permissions.View")
        .WithSummary("Get permission by ID");

        // ============================================
        // 👤 GET MY PERMISSIONS (Current User)
        // ============================================
        group.MapGet("/me", async (
            [FromServices] IPermissionService service,
            [FromServices] IUserContext userContext) =>
        {
            var userIdStr = userContext.GetCurrentUserId();
            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var roleIdStr = userContext.GetCurrentUserRoleId();
            if (!Guid.TryParse(roleIdStr, out var roleId))
                return Results.Unauthorized();

            var permissions = await service.GetUserEffectivePermissionsAsync(userId, roleId);
            return Results.Ok(permissions);
        })
        .RequireAuthorization()
        .WithSummary("Get current user's effective permissions (role + user-specific)");

        // ============================================
        // 🔐 GET PERMISSIONS FOR SPECIFIC USER
        // ============================================
        group.MapGet("/user/{userId:guid}", async (
            Guid userId,
            [FromServices] IPermissionService service,
            [FromServices] IUserContext userContext) =>
        {
            // Get user's role (you'll need to fetch this)
            // For now, returning user-specific permissions only
            var permissions = await service.GetPermissionsByUserAsync(userId);
            return Results.Ok(permissions);
        })
        .RequirePermission("Permissions.View")
        .WithSummary("Get user-specific permission overrides");

        // ============================================
        // 🎭 GET PERMISSIONS FOR ROLE
        // ============================================
        group.MapGet("/role/{roleId:guid}", async (
            Guid roleId,
            [FromServices] IPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByRoleAsync(roleId);
            return Results.Ok(permissions);
        })
        .RequirePermission("Permissions.View")
        .WithSummary("Get all permissions assigned to a role");

        // ============================================
        // ➕ CREATE PERMISSION (SuperAdmin only)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreatePermissionDto dto,
            [FromServices] IPermissionService service) =>
        {
            try
            {
                var created = await service.CreatePermissionAsync(dto);
                return Results.Created($"/api/permissions/{created.Id}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Error = ex.Message });
            }
        })
        .RequirePermission("System.ManageSettings")
        .WithSummary("Create a new permission (SuperAdmin only)");

        // ============================================
        // ✏️ UPDATE PERMISSION
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdatePermissionDto dto,
            [FromServices] IPermissionService service) =>
        {
            var updated = await service.UpdatePermissionAsync(id, dto);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequirePermission("System.ManageSettings")
        .WithSummary("Update permission details");

        // ============================================
        // 🗑️ DELETE PERMISSION
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IPermissionService service) =>
        {
            var deleted = await service.DeletePermissionAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequirePermission("System.ManageSettings")
        .WithSummary("Delete a permission (SuperAdmin only)");

        // ============================================
        // ➕ ASSIGN PERMISSION TO USER (Override)
        // ============================================
        group.MapPost("/assign-user", async (
            [FromBody] AssignPermissionToUserDto dto,
            [FromServices] IPermissionService service) =>
        {
            try
            {
                var assigned = await service.AssignPermissionToUserAsync(dto.UserId, dto.PermissionId);
                return assigned 
                    ? Results.Ok(new { message = "Permission assigned successfully" }) 
                    : Results.Conflict(new { message = "Permission already assigned to this user" });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { Error = ex.Message });
            }
        })
        .RequirePermission("Users.ManagePermissions")
        .WithSummary("Assign permission override to a specific user");

        // ============================================
        // ➖ REMOVE PERMISSION FROM USER
        // ============================================
        group.MapDelete("/user/{userId:guid}/permission/{permissionId:guid}", async (
            Guid userId,
            Guid permissionId,
            [FromServices] IPermissionService service) =>
        {
            var removed = await service.RemovePermissionFromUserAsync(userId, permissionId);
            return removed 
                ? Results.Ok(new { message = "Permission removed successfully" })
                : Results.NotFound();
        })
        .RequirePermission("Users.ManagePermissions")
        .WithSummary("Remove permission override from user");
    }
}