using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Services.Permissions;

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
            var result = await service.GetAllPagedAsync(pagination);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.View)
        .Produces<PaginatedResult<PermissionResponseDto>>(StatusCodes.Status200OK)
        .WithSummary("Get all permissions (paginated)");

        // ============================================
        // 📋 GET ALL PERMISSIONS (NON-PAGED)
        // ============================================
        group.MapGet("/", async ([FromServices] IPermissionService service) =>
        {
            var result = await service.GetAllPermissionsAsync();
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.View)
        .Produces<IEnumerable<PermissionResponseDto>>(StatusCodes.Status200OK)
        .WithSummary("Get all permissions (non-paginated)");

        // ============================================
        // 📊 GET PERMISSIONS GROUPED BY CATEGORY
        // ============================================
        group.MapGet("/grouped", async ([FromServices] IPermissionService service) =>
        {
            var permissions = await service.GetAllPermissionsAsync();

            var grouped = permissions
                .GroupBy(p => p.Name.Split('.')[0])
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Category = g.Key,
                    Permissions = g.OrderBy(p => p.Name).ToList()
                });

            return Results.Ok(grouped);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.View)
        .WithSummary("Get permissions grouped by category");

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
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.View)
        .WithSummary("Get permission by ID");

        // ============================================
        // 👤 GET MY PERMISSIONS (Current User)
        // ============================================
        group.MapGet("/me", async (
            [FromServices] IPermissionService service,
            [FromServices] CachedPermissionService cachedService,
            [FromServices] IUserContext userContext,
            [FromServices] ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Permissions.Me");

            var userIdStr = userContext.GetCurrentUserId();
            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var roleIdStr = userContext.GetCurrentUserRoleId();
            if (!Guid.TryParse(roleIdStr, out var roleId))
                return Results.Unauthorized();

            var permissions = await cachedService.GetUserEffectivePermissionsAsync(userId, roleId);
            var version = cachedService.GetVersion(userId, roleId);

            logger.LogInformation("📊 User {UserId} fetched permissions (Role {RoleId}, Version {Version})", userId, roleId, version);

            return Results.Ok(new
            {
                userId,
                roleId,
                permissions = permissions.Select(p => p.Name).OrderBy(p => p).ToList(),
                permissionsVersion = version
            });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.ViewOwn)
        .WithSummary("Get current user's own permissions (RBAC + cache version)");

        // ============================================
        // 🔐 GET PERMISSIONS FOR SPECIFIC USER
        // ============================================
        group.MapGet("/user/{userId:guid}", async (
            Guid userId,
            [FromServices] IPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByUserAsync(userId);
            return Results.Ok(permissions);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.ViewUserPermissions)
        .WithSummary("Get user-specific permission overrides");

        // ============================================
        // 🔥 GET EFFECTIVE PERMISSIONS FOR ANY USER
        // ============================================
        group.MapGet("/user/{userId:guid}/effective", async (
                Guid userId,
                [FromServices] IPermissionService service,
                [FromServices] CachedPermissionService cachedService) =>
            {
                // جلب role للمستخدم
                var userInfo = await service.GetUserForAuthorizationAsync(userId);
                if (!userInfo.HasValue)
                    return Results.NotFound(new { message = "User not found or has no role" });

                var roleId = userInfo.Value.RoleId;

                // effective = role perms + user override perms
                var effective = await cachedService.GetUserEffectivePermissionsAsync(userId, roleId);

                return Results.Ok(new
                {
                    userId,
                    roleId,
                    permissions = effective.OrderBy(p => p.Name).ToList()
                });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.AppPermissions.ViewUserPermissions)
            .WithSummary("Get the effective permissions (role + overrides) for any user");
        
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
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.View)
        .WithSummary("Get all permissions assigned to a role");

        // ============================================
        // 🔄 REFRESH USER PERMISSIONS (CACHE REBUILD)
        // ============================================
        group.MapPost("/refresh/{userId:guid}", async (
            Guid userId,
            [FromServices] IPermissionService permissionService,
            [FromServices] CachedPermissionService cachedService) =>
        {
            var userInfo = await permissionService.GetUserForAuthorizationAsync(userId);
            if (!userInfo.HasValue)
                return Results.NotFound(new { message = $"User {userId} not found or has no role assigned." });

            cachedService.InvalidateUser(userId, userInfo.Value.RoleId);
            var fresh = await cachedService.GetUserEffectivePermissionsAsync(userId, userInfo.Value.RoleId);
            var version = cachedService.GetVersion(userId, userInfo.Value.RoleId);

            return Results.Ok(new
            {
                message = "✅ Permissions refreshed successfully.",
                userId,
                roleId = userInfo.Value.RoleId,
                permissionsCount = fresh.Count(),
                permissionsVersion = version
            });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Manage)
        .WithSummary("Force refresh cached permissions for a user");

        // ============================================
        // ➕ CREATE PERMISSION (SuperAdmin only)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreatePermissionDto dto,
            [FromServices] IPermissionService service) =>
        {
            var created = await service.CreatePermissionAsync(dto);
            return Results.Created($"/api/permissions/{created.Id}", created);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Create)
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
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Edit)
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
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Delete)
        .WithSummary("Delete a permission (SuperAdmin only)");

        // ============================================
        // ➕ ASSIGN PERMISSION TO USER (Override)
        // ============================================
        group.MapPost("/assign-user", async (
            [FromBody] AssignPermissionToUserDto dto,
            [FromServices] IPermissionService service) =>
        {
            var assigned = await service.AssignPermissionToUserAsync(dto.UserId, dto.PermissionId);
            return assigned
                ? Results.Ok(new { message = "Permission assigned successfully" })
                : Results.Conflict(new { message = "Permission already assigned to this user" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Assign)
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
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Revoke)
        .WithSummary("Remove permission override from user");
        
        // ============================================
        // 🧹 RESET USER PERMISSIONS
        // ============================================
        group.MapDelete("/user/{userId:guid}/reset", async (
                Guid userId,
                [FromServices] IPermissionService service) =>
            {
                // This will call CachedPermissionService.ClearUserPermissionsAsync
                var cleared = await service.ClearUserPermissionsAsync(userId);
    
                return Results.Ok(new { message = "All custom permissions removed. User reverted to Role defaults." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.AppPermissions.Revoke)
            .WithSummary("Remove all custom overrides for a user");
    }
}