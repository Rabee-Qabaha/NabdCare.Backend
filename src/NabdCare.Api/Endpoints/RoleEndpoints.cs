using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Roles;

namespace NabdCare.Api.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles")
            .WithTags("Roles");

        // ============================================
        // 📋 GET ALL ROLES
        // ============================================
        group.MapGet("/", async (
            [FromServices] IRoleService roleService,
            [FromServices] ITenantContext tenantContext) =>
        {
            // SuperAdmin sees all roles (system + templates + all clinic roles)
            // ClinicAdmin sees only their clinic's roles + templates
            var roles = await roleService.GetAllRolesAsync();
            return Results.Ok(roles);
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Get all roles (filtered by tenant context)");

        // ============================================
        // 📋 GET SYSTEM ROLES (SuperAdmin only)
        // ============================================
        group.MapGet("/system", async (
            [FromServices] IRoleService roleService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var roles = await roleService.GetSystemRolesAsync();
            return Results.Ok(roles);
        })
        .RequirePermission("System.ManageRoles")
        .WithSummary("Get system roles (SuperAdmin, SupportManager, BillingManager)");

        // ============================================
        // 📋 GET TEMPLATE ROLES
        // ============================================
        group.MapGet("/templates", async (
            [FromServices] IRoleService roleService) =>
        {
            var roles = await roleService.GetTemplateRolesAsync();
            return Results.Ok(roles);
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Get template roles (for cloning to clinics)");

        // ============================================
        // 📋 GET CLINIC ROLES
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
            Guid clinicId,
            [FromServices] IRoleService roleService,
            [FromServices] ITenantContext tenantContext) =>
        {
            // SuperAdmin can view any clinic's roles
            // ClinicAdmin can only view their own clinic's roles (enforced by service)
            var roles = await roleService.GetClinicRolesAsync(clinicId);
            return Results.Ok(roles);
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Get roles for a specific clinic");

        // ============================================
        // 🔍 GET ROLE BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IRoleService roleService) =>
        {
            var role = await roleService.GetRoleByIdAsync(id);
            return role != null ? Results.Ok(role) : Results.NotFound();
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Get role by ID");

        // ============================================
        // ➕ CREATE CUSTOM ROLE (Clinic-specific)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateRoleRequestDto dto,
            [FromServices] IRoleService roleService,
            [FromServices] IValidator<CreateRoleRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            try
            {
                var created = await roleService.CreateRoleAsync(dto);
                return Results.Created($"/api/roles/{created.Id}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Error = ex.Message });
            }
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Create a custom clinic role");

        // ============================================
        // 🔄 CLONE TEMPLATE ROLE
        // ============================================
        group.MapPost("/clone/{templateRoleId:guid}", async (
            Guid templateRoleId,
            [FromBody] CloneRoleRequestDto dto,
            [FromServices] IRoleService roleService,
            [FromServices] IValidator<CloneRoleRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            try
            {
                var cloned = await roleService.CloneRoleAsync(templateRoleId, dto.ClinicId, dto.NewRoleName);
                return Results.Created($"/api/roles/{cloned.Id}", cloned);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Clone a template role for a clinic");

        // ============================================
        // ✏️ UPDATE ROLE
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateRoleRequestDto dto,
            [FromServices] IRoleService roleService,
            [FromServices] IValidator<UpdateRoleRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            try
            {
                var updated = await roleService.UpdateRoleAsync(id, dto);
                return updated != null ? Results.Ok(updated) : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Update role (cannot update system roles)");

        // ============================================
        // 🗑️ DELETE ROLE
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IRoleService roleService) =>
        {
            try
            {
                var success = await roleService.DeleteRoleAsync(id);
                return success 
                    ? Results.Ok(new { Message = $"Role {id} deleted successfully" }) 
                    : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequirePermission("Settings.ManageRoles")
        .WithSummary("Delete role (cannot delete system roles or roles with users)");

        // ============================================
        // 🔐 GET ROLE PERMISSIONS
        // ============================================
        group.MapGet("/{id:guid}/permissions", async (
            Guid id,
            [FromServices] IRoleService roleService) =>
        {
            var permissions = await roleService.GetRolePermissionsAsync(id);
            return Results.Ok(permissions);
        })
        .RequirePermission("Permissions.View")
        .WithSummary("Get all permissions assigned to a role");

        // ============================================
        // ➕ ASSIGN PERMISSION TO ROLE
        // ============================================
        group.MapPost("/{roleId:guid}/permissions/{permissionId:guid}", async (
            Guid roleId,
            Guid permissionId,
            [FromServices] IRoleService roleService) =>
        {
            try
            {
                var success = await roleService.AssignPermissionToRoleAsync(roleId, permissionId);
                return success 
                    ? Results.Ok(new { Message = "Permission assigned successfully" })
                    : Results.Conflict(new { Message = "Permission already assigned" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequirePermission("Permissions.Assign")
        .WithSummary("Assign a permission to a role");

        // ============================================
        // ➖ REMOVE PERMISSION FROM ROLE
        // ============================================
        group.MapDelete("/{roleId:guid}/permissions/{permissionId:guid}", async (
            Guid roleId,
            Guid permissionId,
            [FromServices] IRoleService roleService) =>
        {
            try
            {
                var success = await roleService.RemovePermissionFromRoleAsync(roleId, permissionId);
                return success 
                    ? Results.Ok(new { Message = "Permission removed successfully" })
                    : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequirePermission("Permissions.Revoke")
        .WithSummary("Remove a permission from a role");
    }
}