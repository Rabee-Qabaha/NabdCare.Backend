using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Production-ready user management endpoints with cursor-based pagination,
/// consistent permission naming, and clean architecture design.
/// Author: Rabee Qabaha
/// Updated: 2025-10-29 22:30 UTC
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users")
            .WithTags("Users");

        // ============================================
        // CREATE USER
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<CreateUserRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var user = await userService.CreateUserAsync(dto);
            return Results.Created($"/api/users/{user.Id}", user);
        })
        .RequirePermission("Users.Create")
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .WithDescription("SuperAdmin: Create user in any clinic. ClinicAdmin: Create user in own clinic only.")
        .Produces<UserResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status409Conflict);

        // ============================================
        // GET PAGED USERS (Multi-Tenant Smart Logic)
        // ============================================
        group.MapGet("/paged", async (
            [FromQuery] int limit,
            [FromQuery] string? cursor,
            [FromQuery] Guid? clinicId,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (limit <= 0)
                return Results.BadRequest(new { Error = "Limit must be greater than 0" });

            PaginatedResult<UserResponseDto> result;

            if (tenantContext.IsSuperAdmin)
            {
                // SuperAdmin can view all users, or filter by clinic
                result = clinicId.HasValue
                    ? await userService.GetByClinicIdPagedAsync(clinicId.Value, limit, cursor)
                    : await userService.GetAllPagedAsync(limit, cursor);
            }
            else if (tenantContext.ClinicId.HasValue)
            {
                // ClinicAdmin can only view their own clinic
                result = await userService.GetByClinicIdPagedAsync(tenantContext.ClinicId.Value, limit, cursor);
            }
            else
            {
                return Results.Json(
                    new { Error = "You don't have permission to view users" },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(result);
        })
        .RequirePermission("Users.View")
        .WithName("GetPagedUsers")
        .WithSummary("Get paged users (multi-tenant aware)")
        .WithDescription("Cursor-based pagination for users. SuperAdmin: all or by clinic. ClinicAdmin: own clinic only.")
        .Produces<PaginatedResult<UserResponseDto>>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET USERS BY CLINIC (SuperAdmin Only, Paged)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/paged", async (
            Guid clinicId,
            [FromQuery] int limit,
            [FromQuery] string? cursor,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (clinicId == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            if (limit <= 0)
                return Results.BadRequest(new { Error = "Limit must be greater than 0" });

            if (!tenantContext.IsSuperAdmin)
                return Results.Json(
                    new { Error = "Only SuperAdmin can view other clinics' users" },
                    statusCode: StatusCodes.Status403Forbidden);

            var result = await userService.GetByClinicIdPagedAsync(clinicId, limit, cursor);
            return Results.Ok(result);
        })
        .RequirePermission("Users.ViewAll")
        .WithName("GetUsersByClinicPaged")
        .WithSummary("Get users in a specific clinic (paged, SuperAdmin only)")
        .Produces<PaginatedResult<UserResponseDto>>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET USER BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var user = await userService.GetUserByIdAsync(id);
            return user is not null
                ? Results.Ok(user)
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.ViewDetails")
        .WithName("GetUserById")
        .WithSummary("Get user by ID")
        .WithDescription("Returns detailed user info. Automatically respects multi-tenant access control.")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET CURRENT USER (Me)
        // ============================================
        group.MapGet("/me", async (
            [FromServices] IUserService userService,
            [FromServices] IUserContext userContext) =>
        {
            var userIdStr = userContext.GetCurrentUserId();

            if (string.IsNullOrEmpty(userIdStr) || userIdStr == "anonymous")
                return Results.Json(
                    new { Error = "User not authenticated" },
                    statusCode: StatusCodes.Status401Unauthorized);

            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Json(
                    new { Error = "Invalid user ID format" },
                    statusCode: StatusCodes.Status400BadRequest);

            var user = await userService.GetUserByIdAsync(userId);
            return user is not null
                ? Results.Ok(user)
                : Results.NotFound(new { Error = "User not found" });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get current authenticated user's info")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // UPDATE USER (keep same as before)
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<UpdateUserRequestDto> validator) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updatedUser = await userService.UpdateUserAsync(id, dto);
            return updatedUser is not null
                ? Results.Ok(updatedUser)
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.Edit")
        .WithName("UpdateUser")
        .WithSummary("Update user details")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // UPDATE USER ROLE
        // ============================================
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<UpdateUserRoleDto> validator) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updatedUser = await userService.UpdateUserRoleAsync(id, dto.RoleId);
            return updatedUser is not null 
                ? Results.Ok(updatedUser) 
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.ChangeRole")  // ✅ FIXED: Changed from Users.ManageRoles
        .WithName("UpdateUserRole")
        .WithSummary("Update user's role")
        .WithDescription("Change a user's role. ClinicAdmin: Only within clinic. SuperAdmin: Any user.")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // ACTIVATE USER
        // ============================================
        group.MapPut("/{id:guid}/activate", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var user = await userService.ActivateUserAsync(id);
            return user is not null 
                ? Results.Ok(new { Message = $"User {id} activated successfully", User = user }) 
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.Activate")
        .WithName("ActivateUser")
        .WithSummary("Activate a deactivated user")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // DEACTIVATE USER
        // ============================================
        group.MapPut("/{id:guid}/deactivate", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var user = await userService.DeactivateUserAsync(id);
            return user is not null 
                ? Results.Ok(new { Message = $"User {id} deactivated successfully", User = user }) 
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.Activate")
        .WithName("DeactivateUser")
        .WithSummary("Deactivate a user (prevents login)")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // SOFT DELETE USER
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var success = await userService.SoftDeleteUserAsync(id);
            return success 
                ? Results.Ok(new { Message = $"User {id} soft deleted successfully" }) 
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.Delete")
        .WithName("SoftDeleteUser")
        .WithSummary("Soft delete a user")
        .WithDescription("Marks user as deleted. Can be restored. ClinicAdmin: Only clinic users.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // HARD DELETE USER (SuperAdmin Only)
        // ============================================
        group.MapDelete("/{id:guid}/permanent", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            if (!tenantContext.IsSuperAdmin)
                return Results.Json(
                    new { Error = "Missing required permission: Users.Delete" },
                    statusCode: StatusCodes.Status403Forbidden);

            var success = await userService.HardDeleteUserAsync(id);
            return success 
                ? Results.Ok(new { Message = $"User {id} permanently deleted" }) 
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequirePermission("Users.Delete")  // ✅ FIXED: Uses Users.Delete (not Users.HardDelete)
        .WithName("HardDeleteUser")
        .WithSummary("Permanently delete a user (SuperAdmin only - DANGEROUS)")
        .WithDescription("⚠️ IRREVERSIBLE: Completely removes user from database.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // CHANGE PASSWORD (Self)
        // ============================================
        group.MapPost("/{id:guid}/change-password", async (
            Guid id,
            [FromBody] ChangePasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ChangePasswordRequestDto> validator) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await userService.ChangePasswordAsync(id, dto);
            return Results.Ok(new { Message = "Password changed successfully", User = updated });
        })
        .RequireAuthorization()
        .WithName("ChangePassword")
        .WithSummary("Change own password")
        .WithDescription("User changes their own password. Requires current password.")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // RESET PASSWORD (ClinicAdmin)
        // ============================================
        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ResetPasswordRequestDto> validator) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await userService.ResetPasswordAsync(id, dto);
            return Results.Ok(new { Message = "Password reset successfully", User = updated });
        })
        .RequirePermission("Users.ResetPassword")
        .WithName("ResetUserPassword")
        .WithSummary("Reset user password (ClinicAdmin)")
        .WithDescription("ClinicAdmin resets password for users in their clinic. No current password required.")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // ADMIN RESET PASSWORD (SuperAdmin)
        // ============================================
        group.MapPost("/{id:guid}/admin-reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ResetPasswordRequestDto> validator,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid user ID" });

            if (!tenantContext.IsSuperAdmin)
                return Results.Json(
                    new { Error = "Missing required permission: Users.ResetPassword" },
                    statusCode: StatusCodes.Status403Forbidden);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await userService.AdminResetPasswordAsync(id, dto);
            return Results.Ok(new { Message = "Password reset by SuperAdmin", User = updated });
        })
        .RequirePermission("Users.ResetPassword")  // ✅ FIXED: Uses Users.ResetPassword (not AdminResetPassword)
        .WithName("AdminResetPassword")
        .WithSummary("SuperAdmin resets any user's password")
        .WithDescription("SuperAdmin can reset password for any user in any clinic.")
        .Produces<UserResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}