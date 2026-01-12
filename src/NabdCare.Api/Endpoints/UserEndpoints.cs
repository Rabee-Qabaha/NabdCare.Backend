using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users").WithTags("Users");

        // ============================================
        // ➕ CREATE USER
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<CreateUserRequestDto> validator,
            [FromServices] ITenantContext tenantContext) =>
        {
            // 1. Validation
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);

            // 2. Enforce Tenant Scope
            if (!tenantContext.IsSuperAdmin)
            {
                // Clinic Admins can ONLY create users for their own clinic
                dto.ClinicId = tenantContext.ClinicId!.Value;
            }

            // 3. Create (Service handles Permissions, Limits & Logic)
            var user = await userService.CreateUserAsync(dto);
            return Results.Created($"/api/users/{user.Id}", user);
        })
        .RequireAuthorization()
        // Note: We don't use .RequirePermission here because the Service handles the 
        // complex "SuperAdmin vs ClinicAdmin" permission check internally.
        .WithName("CreateUser")
        .Produces<UserResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 📋 GET PAGED USERS (Hybrid)
        // ============================================
        group.MapGet("/paged", async (
                [AsParameters] UserFilterRequestDto filter,
                [FromServices] IUserService userService) =>
            {
                var result = await userService.GetAllPagedAsync(filter);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithName("GetPagedUsers");

        // ============================================
        // 📋 GET USERS BY CLINIC (Scoped)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/paged", async (
            Guid clinicId,
            [AsParameters] PaginationRequestDto request,
            [FromQuery] bool includeDeleted,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IPermissionEvaluator perms) =>
        {
            // 🔐 Security: System Admin OR Own Clinic with Permission
            var isSystem = tenantContext.IsSuperAdmin;
            var isOwner = tenantContext.ClinicId == clinicId;

            if (!isSystem && !isOwner) return Results.Forbid();
            
            if (isOwner && !await perms.HasAsync(Permissions.Users.View))
                return Results.Forbid();

            var result = await userService.GetByClinicIdPagedAsync(clinicId, request.Limit, request.Cursor, includeDeleted);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("GetUsersByClinicPaged");

        // ============================================
        // 🔍 GET USER BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenant,
            [FromServices] IUserContext userCtx,
            [FromServices] IPermissionEvaluator perms) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            if (user == null) return Results.NotFound();

            // 🔐 Security: Self OR Admin OR Viewer in same clinic
            var isSelf = user.Id.ToString() == userCtx.GetCurrentUserId();
            var isSystem = tenant.IsSuperAdmin;
            var isSameClinic = tenant.ClinicId == user.ClinicId;
            var hasViewPerm = await perms.HasAsync(Permissions.Users.ViewDetails);

            if (isSelf || isSystem || (isSameClinic && hasViewPerm))
                return Results.Ok(user);

            return Results.Forbid();
        })
        .RequireAuthorization()
        .WithName("GetUserById");

        // ============================================
        // 👤 GET CURRENT USER (Me)
        // ============================================
        group.MapGet("/me", async (
            [FromServices] IUserService userService,
            [FromServices] IUserContext userContext) =>
        {
            // No ID check needed, Service gets it from Context
            var user = await userService.GetCurrentUserAsync();
            return user != null ? Results.Ok(user) : Results.Unauthorized();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser");

        // ============================================
        // ✅ CHECK EMAIL STATUS
        // ============================================
        group.MapGet("/check-email", async (
            [FromQuery] string email,
            [FromServices] IUserService userService) =>
        {
            if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest(new { Error = "Email is required" });

            var (exists, isDeleted, userId) = await userService.EmailExistsDetailedAsync(email);
            return Results.Ok(new { Exists = exists, IsDeleted = isDeleted, UserId = userId });
        })
        .RequireAuthorization() // Or .AllowAnonymous() if used during public registration
        .WithName("CheckEmailStatus");

        // ============================================
        // ♻️ RESTORE USER
        // ============================================
        group.MapPut("/{id:guid}/restore", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            // Service handles Permissions (Restore) and Limits (Subscription)
            var restored = await userService.RestoreUserAsync(id);
            return restored != null ? Results.Ok(restored) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("RestoreUser");

        // ============================================
        // ✏️ UPDATE USER
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<UpdateUserRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);

            var updatedUser = await userService.UpdateUserAsync(id, dto);
            return updatedUser != null ? Results.Ok(updatedUser) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateUser");

        // ============================================
        // 🎭 UPDATE USER ROLE
        // ============================================
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.UpdateUserRoleAsync(id, dto.RoleId);
            return updatedUser != null ? Results.Ok(updatedUser) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateUserRole");

        // ============================================
        // ▶️ ACTIVATE USER
        // ============================================
        group.MapPut("/{id:guid}/activate", async (Guid id, [FromServices] IUserService svc) =>
        {
            var u = await svc.ActivateUserAsync(id);
            return u != null ? Results.Ok(u) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ActivateUser");

        // ============================================
        // ⏸️ DEACTIVATE USER
        // ============================================
        group.MapPut("/{id:guid}/deactivate", async (Guid id, [FromServices] IUserService svc) =>
        {
            var u = await svc.DeactivateUserAsync(id);
            return u != null ? Results.Ok(u) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("DeactivateUser");

        // ============================================
        // 🗑️ SOFT DELETE
        // ============================================
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IUserService svc) =>
        {
            var ok = await svc.SoftDeleteUserAsync(id);
            return ok ? Results.Ok(new { Message = "User soft deleted" }) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("DeleteUser");

        // ============================================
        // 💥 HARD DELETE (Admin)
        // ============================================
        group.MapDelete("/{id:guid}/permanent", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            var success = await userService.HardDeleteUserAsync(id);
            return success ? Results.Ok(new { Message = "User permanently deleted" }) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("HardDeleteUser");

        // ============================================
        // 🔑 PASSWORD OPS
        // ============================================
        group.MapPost("/{id:guid}/change-password", async (
            Guid id,
            [FromBody] ChangePasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ChangePasswordRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);

            var updated = await userService.ChangePasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .WithName("ChangePassword");

        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ResetPasswordRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);

            var updated = await userService.ResetPasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .WithName("ResetPassword");
    }
}