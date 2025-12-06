using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Api.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("roles")
            .WithTags("Roles");

        // ============================================
        // 📋 GET ALL ROLES (with filtering)
        // ============================================
        group.MapGet("/", async (
                [FromServices] IRoleService service,
                [FromServices] ITenantContext tenant,
                // 👇 FIX: Add default values to make query params optional
                [FromQuery] bool includeDeleted = false, 
                [FromQuery] Guid? clinicId = null) =>
            {
                // SuperAdmin → all roles (or filtered by specific clinicId)
                // ClinicAdmin → only own clinic (force override)
                if (!tenant.IsSuperAdmin)
                    clinicId = tenant.ClinicId;

                var roles = await service.GetAllRolesAsync(includeDeleted, clinicId);
                return Results.Ok(roles);
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.ViewAll)
            .WithAbac<Role>(
                Permissions.Roles.ViewAll,
                "view",
                r => r as Role)
            .WithName("GetAllRoles")
            .WithSummary("Get all roles with filtering options")
            .Produces<IEnumerable<RoleResponseDto>>(StatusCodes.Status200OK);

        // ============================================
        // 📋 GET SYSTEM ROLES
        // ============================================
        group.MapGet("/system", async (
            [FromServices] IRoleService service) =>
        {
            var roles = await service.GetSystemRolesAsync();
            return Results.Ok(roles);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.ViewSystem)
        .WithAbac<Role>(
            Permissions.Roles.ViewSystem,
            "viewSystem",
            r => r as Role)
        .WithName("GetSystemRoles")
        .WithSummary("Get system-defined roles");

        // ============================================
        // 📋 GET TEMPLATE ROLES
        // ============================================
        group.MapGet("/templates", async (
            [FromServices] IRoleService service) =>
        {
            var roles = await service.GetTemplateRolesAsync();
            return Results.Ok(roles);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.ViewTemplates)
        .WithAbac<Role>(
            Permissions.Roles.ViewTemplates,
            "viewTemplates",
            r => r as Role)
        .WithName("GetTemplateRoles")
        .WithSummary("Get template roles");

        // ============================================
        // 📋 GET CLINIC ROLES
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
            Guid clinicId,
            [FromServices] IRoleService service,
            [FromServices] ITenantContext tenant) =>
        {
            if (!tenant.IsSuperAdmin)
                clinicId = tenant.ClinicId!.Value; // enforce own clinic

            var roles = await service.GetClinicRolesAsync(clinicId);
            return Results.Ok(roles);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.ViewClinic)
        .WithAbac<Role>(
            Permissions.Roles.ViewClinic,
            "viewClinic",
            r => r as Role)
        .WithName("GetClinicRoles")
        .WithSummary("Get roles for a specific clinic");

        // ============================================
        // 🔍 GET ROLE BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IRoleService service) =>
        {
            var role = await service.GetRoleByIdAsync(id);
            return role is not null ? Results.Ok(role) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.View)
        .WithAbac(
            Permissions.Roles.View,
            "view",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IRoleService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var rid)
                    ? await svc.GetRoleByIdAsync(rid)
                    : null;
            })
        .WithName("GetRoleById");

        // ============================================
        // ➕ CREATE ROLE
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateRoleRequestDto dto,
            [FromServices] IRoleService service,
            [FromServices] ITenantContext tenant) =>
        {
            // ClinicAdmin can only create roles inside own clinic
            if (!tenant.IsSuperAdmin)
                dto.ClinicId = tenant.ClinicId!.Value;

            var created = await service.CreateRoleAsync(dto);
            return Results.Created($"/api/roles/{created.Id}", created);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.Create)
        .WithAbac<Role>(
            Permissions.Roles.Create,
            "create",
            r => r as Role)
        .WithName("CreateRole")
        .Produces<RoleResponseDto>(StatusCodes.Status201Created);

        // ============================================
        // 🔄 CLONE TEMPLATE ROLE
        // ============================================
        group.MapPost("/{templateRoleId:guid}/clone", async (
                Guid templateRoleId,
                [FromBody] CloneRoleRequestDto dto,
                [FromServices] IRoleService service,
                [FromServices] ITenantContext tenant) =>
            {
                // Security: Enforce clinic ID for non-superadmins
                if (!tenant.IsSuperAdmin)
                    dto.ClinicId = tenant.ClinicId!.Value;

                // Pass the full DTO to the service
                var cloned = await service.CloneRoleAsync(templateRoleId, dto);
    
                return Results.Created($"/api/roles/{cloned.Id}", cloned);
            })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.Clone)
        .WithAbac<Role>(
            Permissions.Roles.Clone,
            "clone",
            r => r as Role)
        .WithName("CloneRole");

        // ============================================
        // ✏️ UPDATE ROLE
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateRoleRequestDto dto,
            [FromServices] IRoleService service) =>
        {
            var updated = await service.UpdateRoleAsync(id, dto);
            return updated is not null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.Edit)
        .WithAbac(
            Permissions.Roles.Edit,
            "edit",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IRoleService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var rid)
                    ? await svc.GetRoleByIdAsync(rid)
                    : null;
            })
        .WithName("UpdateRole");

        // ============================================
        // 🗑️ SOFT DELETE ROLE (Move to Trash)
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IRoleService service) =>
        {
            var success = await service.SoftDeleteRoleAsync(id);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.Delete)
        .WithAbac(
            Permissions.Roles.Delete,
            "delete",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IRoleService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var rid)
                    ? await svc.GetRoleByIdAsync(rid)
                    : null;
            })
        .WithName("SoftDeleteRole")
        .WithSummary("Soft delete a role (move to trash).");

        // ============================================
// 💥 HARD DELETE ROLE (Permanent)
// ============================================
        group.MapDelete("/{id:guid}/permanent", async (
                Guid id,
                [FromServices] IRoleService service) =>
            {
                var success = await service.HardDeleteRoleAsync(id);
                return success ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.HardDelete)
            .WithAbac<RoleResponseDto>( // ✅ FIX: Use DTO Type
                Permissions.Roles.HardDelete,
                "hardDelete",
                async ctx =>
                {
                    var service = ctx.RequestServices.GetRequiredService<IRoleService>();
                    var idStr = ctx.Request.RouteValues["id"]?.ToString();

                    return Guid.TryParse(idStr, out var rid)
                        ? await service.GetRoleByIdAsync(rid) 
                        : null;
                })
            .WithName("HardDeleteRole")
            .WithSummary("Permanently delete a role (irreversible).");

        // ============================================
        // ♻️ RESTORE ROLE
        // ============================================
        group.MapPost("/{id:guid}/restore", async (
            Guid id,
            [FromServices] IRoleService service) =>
        {
            var restored = await service.RestoreRoleAsync(id);
            return restored is not null ? Results.Ok(restored) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Roles.Restore)
        .WithAbac(
            Permissions.Roles.Restore,
            "restore",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IRoleService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var rid)
                    ? await svc.GetRoleByIdAsync(rid)
                    : null;
            })
        .WithName("RestoreRole");

        // ============================================
        // 🔐 GET ROLE PERMISSIONS
        // ============================================
        group.MapGet("/{id:guid}/permissions", async (
            Guid id,
            [FromServices] IRoleService service) =>
        {
            var perms = await service.GetRolePermissionsAsync(id);
            return Results.Ok(perms);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.View)
        .WithAbac<Role>(
            Permissions.AppPermissions.View,
            "viewPermissions",
            r => r as Role)
        .WithName("GetRolePermissions");

        // ============================================
        // ➕ ASSIGN PERMISSION TO ROLE
        // ============================================
        group.MapPost("/{roleId:guid}/permissions/{permissionId:guid}", async (
            Guid roleId,
            Guid permissionId,
            [FromServices] IRoleService service) =>
        {
            var ok = await service.AssignPermissionToRoleAsync(roleId, permissionId);
            return ok
                ? Results.Ok(new { message = "Permission assigned" })
                : Results.Conflict(new { message = "Already assigned" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Assign)
        .WithAbac(
            Permissions.AppPermissions.Assign,
            "assignPermission",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IRoleService>();
                var idStr = ctx.Request.RouteValues["roleId"]?.ToString();
                return Guid.TryParse(idStr, out var rid)
                    ? await svc.GetRoleByIdAsync(rid)
                    : null;
            })
        .WithName("AssignPermission");

        // ============================================
        // ➖ REMOVE PERMISSION FROM ROLE
        // ============================================
        group.MapDelete("/{roleId:guid}/permissions/{permissionId:guid}", async (
            Guid roleId,
            Guid permissionId,
            [FromServices] IRoleService service) =>
        {
            var ok = await service.RemovePermissionFromRoleAsync(roleId, permissionId);
            return ok ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AppPermissions.Revoke)
        .WithAbac(
            Permissions.AppPermissions.Revoke,
            "revokePermission",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IRoleService>();
                var idStr = ctx.Request.RouteValues["roleId"]?.ToString();
                return Guid.TryParse(idStr, out var rid)
                    ? await svc.GetRoleByIdAsync(rid)
                    : null;
            })
        .WithName("RemovePermission");
    }
}