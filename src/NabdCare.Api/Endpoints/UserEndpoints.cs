using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;

namespace NabdCare.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // ✅ Create a new user
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
        .RequirePermission("CreateUser")
        .WithSummary("Create a new user in the current clinic (SuperAdmin can specify ClinicId)");

        // ✅ Get user by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization()
        .WithSummary("Get user by ID (auto filtered by global filter)");

        // ✅ Get all users (auto filtered by global filter)
        group.MapGet("/", async ([FromServices] IUserService userService) =>
        {
            // Global filters in DbContext handle clinic + soft-delete
            var users = await userService.GetUsersByClinicIdAsync(null);
            return Results.Ok(users);
        })
        .RequirePermission("ViewUsers")
        .WithSummary("Get all users (SuperAdmin sees all, ClinicAdmin sees clinic’s users)");

        // ✅ Optional: SuperAdmin only - View specific clinic users
        group.MapGet("/clinic/{clinicId:guid}", async (
            Guid clinicId,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var users = await userService.GetUsersByClinicIdAsync(clinicId);
            return Results.Ok(users);
        })
        .RequirePermission("ViewUsers")
        .WithSummary("SuperAdmin can view users of a specific clinic");

        // ✅ Update user info
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<UpdateUserRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updatedUser = await userService.UpdateUserAsync(id, dto);
            return updatedUser is not null ? Results.Ok(updatedUser) : Results.NotFound($"User {id} not found");
        })
        .RequirePermission("UpdateUser")
        .WithSummary("Update user details (SuperAdmin or clinic-level user)");

        // ✅ Update user role
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<UpdateUserRoleDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updatedUser = await userService.UpdateUserRoleAsync(id, dto.Role);
            return updatedUser is not null ? Results.Ok(updatedUser) : Results.NotFound($"User {id} not found");
        })
        .RequirePermission("UpdateUserRole")
        .WithSummary("Update a user’s role (SuperAdmin or clinic admin)");

        // ✅ Soft delete user
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            var success = await userService.SoftDeleteUserAsync(id);
            return success ? Results.Ok($"User {id} soft deleted.") : Results.NotFound($"User {id} not found.");
        })
        .RequirePermission("DeleteUser")
        .WithSummary("Soft delete a user");

        // ✅ Change password (self)
        group.MapPost("/{id:guid}/change-password", async (
            Guid id,
            [FromBody] ChangePasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ChangePasswordRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await userService.ChangePasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .WithSummary("User changes their own password");

        // ✅ Reset password (clinic admin only)
        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ResetPasswordRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await userService.ResetPasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequirePermission("ResetPassword")
        .WithSummary("ClinicAdmin resets password for users in their clinic");

        // ✅ Admin reset password (super admin only)
        group.MapPost("/{id:guid}/admin-reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<ResetPasswordRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await userService.AdminResetPasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequirePermission("AdminResetPassword")
        .WithSummary("SuperAdmin resets any user’s password (any clinic)");
    }
}