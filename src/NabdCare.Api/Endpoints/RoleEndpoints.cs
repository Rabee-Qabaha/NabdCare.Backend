using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// API Endpoints for Role Management
/// 
/// Author: Rabee-Qabaha
/// Updated: 2025-11-10 19:47:33 UTC
/// </summary>
public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles").WithTags("Roles");

        // ============================================
        // 📋 GET ALL ROLES (with filtering)
        // ============================================
        group.MapGet("/", GetAllRoles)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.ViewAll)
            .WithAbac<Role>(Permissions.Roles.ViewAll, "view", r => r as Role)
            .WithName("GetAllRoles")
            .WithOpenApi()
            .WithSummary("Get all roles with optional filtering")
            .WithDescription("Retrieve all roles accessible to the current user. Supports filtering by deletion status and clinic.");

        // ============================================
        // 📋 GET SYSTEM ROLES (SuperAdmin only)
        // ============================================
        group.MapGet("/system", GetSystemRoles)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.ViewSystem)
            .WithAbac<Role>(Permissions.Roles.ViewSystem, "viewSystem", r => r as Role)
            .WithName("GetSystemRoles")
            .WithOpenApi()
            .WithSummary("Get system roles (SuperAdmin, SupportManager, BillingManager)")
            .WithDescription("Retrieve system roles. Only accessible to SuperAdmin users.");

        // ============================================
        // 📋 GET TEMPLATE ROLES
        // ============================================
        group.MapGet("/templates", GetTemplateRoles)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.ViewTemplates)
            .WithAbac<Role>(Permissions.Roles.ViewTemplates, "viewTemplates", r => r as Role)
            .WithName("GetTemplateRoles")
            .WithOpenApi()
            .WithSummary("Get template roles")
            .WithDescription("Retrieve template roles that can be cloned by clinics.");

        // ============================================
        // 📋 GET CLINIC ROLES
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", GetClinicRoles)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.ViewClinic)
            .WithAbac<Role>(Permissions.Roles.ViewClinic, "viewClinic", r => r as Role)
            .WithName("GetClinicRoles")
            .WithOpenApi()
            .WithSummary("Get roles for a specific clinic")
            .WithDescription("Retrieve roles for a specific clinic. User can only access their own clinic's roles.");

        // ============================================
        // 🔍 GET ROLE BY ID
        // ============================================
        group.MapGet("/{id:guid}", GetRoleById)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.View)
            .WithAbac<Role>(Permissions.Roles.View, "view", r => r as Role)
            .WithName("GetRoleById")
            .WithOpenApi()
            .WithSummary("Get role by ID")
            .WithDescription("Retrieve a specific role by its ID.");

        // ============================================
        // ➕ CREATE CUSTOM ROLE
        // ============================================
        group.MapPost("/", CreateRole)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.Create)
            .WithAbac<Role>(Permissions.Roles.Create, "create", r => r as Role)
            .WithName("CreateRole")
            .WithOpenApi()
            .WithSummary("Create a custom clinic role")
            .WithDescription("Create a new custom role for a clinic.");

        // ============================================
        // 🔄 CLONE TEMPLATE ROLE
        // ============================================
        group.MapPost("/{templateRoleId:guid}/clone", CloneRole)
            .RequireAuthorization()
            .RequirePermission(Permissions.Roles.Clone)
            .WithAbac<Role>(Permissions.Roles.Clone, "clone", r => r as Role)
            .WithName("CloneRole")
            .WithOpenApi()
            .WithSummary("Clone a template role for a clinic")
            .WithDescription("Clone a template role to create a new role for a clinic with the same permissions.");

        // ============================================
        // ✏️ UPDATE ROLE
        // ============================================
        group.MapPut("/{id:guid}", UpdateRole)
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
            .WithName("UpdateRole")
            .WithOpenApi()
            .WithSummary("Update role")
            .WithDescription("Update an existing role. Cannot update system roles.");

        // ============================================
        // 🗑️ DELETE ROLE (Soft Delete)
        // ============================================
        group.MapDelete("/{id:guid}", DeleteRole)
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
            .WithName("DeleteRole")
            .WithOpenApi()
            .WithSummary("Delete role (soft delete)")
            .WithDescription("Soft delete a role. Cannot delete system roles or roles with assigned users.");

        // ============================================
        // ✅ RESTORE ROLE
        // ============================================
        group.MapPost("/{id:guid}/restore", RestoreRole)
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
            .WithName("RestoreRole")
            .WithOpenApi()
            .WithSummary("Restore a soft-deleted role")
            .WithDescription("Restore a previously soft-deleted role.");

        // ============================================
        // 🔐 GET ROLE PERMISSIONS
        // ============================================
        group.MapGet("/{id:guid}/permissions", GetRolePermissions)
            .RequireAuthorization()
            .RequirePermission(Permissions.AppPermissions.View)
            .WithAbac<Role>(Permissions.AppPermissions.View, "viewPermissions", r => r as Role)
            .WithName("GetRolePermissions")
            .WithOpenApi()
            .WithSummary("Get role permissions")
            .WithDescription("Retrieve all permissions assigned to a role.");

        // ============================================
        // ➕ ASSIGN PERMISSION TO ROLE
        // ============================================
        group.MapPost("/{roleId:guid}/permissions/{permissionId:guid}", AssignPermission)
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
            .WithName("AssignPermission")
            .WithOpenApi()
            .WithSummary("Assign permission to role")
            .WithDescription("Assign a permission to a role.");

        // ============================================
        // ➖ REMOVE PERMISSION FROM ROLE
        // ============================================
        group.MapDelete("/{roleId:guid}/permissions/{permissionId:guid}", RemovePermission)
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
            .WithName("RemovePermission")
            .WithOpenApi()
            .WithSummary("Remove permission from role")
            .WithDescription("Remove a permission from a role.");
    }

    // ============================================
    // ENDPOINT HANDLERS
    // ============================================

    private static async Task<IResult> GetAllRoles(
        [FromServices] IRoleService roleService,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] Guid? clinicId = null)
    {
        try
        {
            var roles = await roleService.GetAllRolesAsync(includeDeleted, clinicId);
            return Results.Ok(roles);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> GetSystemRoles(
        [FromServices] IRoleService roleService)
    {
        try
        {
            var roles = await roleService.GetSystemRolesAsync();
            return Results.Ok(roles);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> GetTemplateRoles(
        [FromServices] IRoleService roleService)
    {
        try
        {
            var roles = await roleService.GetTemplateRolesAsync();
            return Results.Ok(roles);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> GetClinicRoles(
        Guid clinicId,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var roles = await roleService.GetClinicRolesAsync(clinicId);
            return Results.Ok(roles);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> GetRoleById(
        Guid id,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var role = await roleService.GetRoleByIdAsync(id);
            return role != null ? Results.Ok(role) : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> CreateRole(
        [FromBody] CreateRoleRequestDto dto,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var created = await roleService.CreateRoleAsync(dto);
            return Results.Created($"/api/roles/{created.Id}", created);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return Results.BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> CloneRole(
        Guid templateRoleId,
        [FromBody] CloneRoleRequestDto dto,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var cloned = await roleService.CloneRoleAsync(templateRoleId, dto.ClinicId, dto.NewRoleName);
            return Results.Created($"/api/roles/{cloned.Id}", cloned);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return Results.BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequestDto dto,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var updated = await roleService.UpdateRoleAsync(id, dto);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return Results.BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRole(
        Guid id,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var deleted = await roleService.DeleteRoleAsync(id);
            return deleted != null ? Results.Ok(deleted) : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> RestoreRole(
        Guid id,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var restored = await roleService.RestoreRoleAsync(id);
            return restored != null ? Results.Ok(restored) : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> GetRolePermissions(
        Guid id,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var permissions = await roleService.GetRolePermissionsAsync(id);
            return Results.Ok(permissions);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> AssignPermission(
        Guid roleId,
        Guid permissionId,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var success = await roleService.AssignPermissionToRoleAsync(roleId, permissionId);
            return success
                ? Results.Ok(new { Message = "Permission assigned successfully" })
                : Results.Conflict(new { Message = "Permission already assigned" });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> RemovePermission(
        Guid roleId,
        Guid permissionId,
        [FromServices] IRoleService roleService)
    {
        try
        {
            var success = await roleService.RemovePermissionFromRoleAsync(roleId, permissionId);
            return success
                ? Results.Ok(new { Message = "Permission removed successfully" })
                : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }
}