using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;

namespace NabdCare.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // Create User
        group.MapPost("/", async (
            [FromBody] CreateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<CreateUserRequestDto> validator) =>
        {
            var validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return Results.BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var user = await userService.CreateUserAsync(dto);
            return Results.Created($"/api/users/{user.Id}", user);
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithSummary("Create a new user");

        // Get user by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            return user != null ? Results.Ok(user) : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization()
        .WithSummary("Get user by ID");

        // Get users by clinic
        group.MapGet("/", async (
            [FromServices] IUserService userService) =>
        {
            var users = await userService.GetUsersByClinicIdAsync(null);
            return Results.Ok(users);
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithSummary("Get all users in accessible clinics");

        // Update user info
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.UpdateUserAsync(id, dto);
            return updatedUser != null ? Results.Ok(updatedUser) : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithSummary("Update user details");

        // Update user role
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.UpdateUserRoleAsync(id, dto.Role);
            return updatedUser != null ? Results.Ok(updatedUser) : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .RequirePermission("UpdateUserRole")
        .WithSummary("Update user's role");

        // Soft delete user
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService) =>
        {
            var success = await userService.SoftDeleteUserAsync(id);
            return success ? Results.Ok($"User with ID {id} has been soft deleted.") : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization("SuperAdmin")
        .RequirePermission("DeleteUser")
        .WithSummary("Soft delete a user");

        // Change password (self)
        group.MapPost("/{id:guid}/change-password", async (
            Guid id,
            [FromBody] ChangePasswordRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.ChangePasswordAsync(id, dto);
            return Results.Ok(updatedUser);
        })
        .RequireAuthorization()
        .WithSummary("Change user's own password");

        // Reset password (clinic admin for clinic users)
        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.ResetPasswordAsync(id, dto);
            return updatedUser != null ? Results.Ok(updatedUser) : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization("ClinicAdmin")
        .RequirePermission("ResetPassword")
        .WithSummary("Reset password for users in your clinic");

        // Admin reset password (super admin for any user)
        group.MapPost("/{id:guid}/admin-reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.AdminResetPasswordAsync(id, dto);
            return Results.Ok(updatedUser);
        })
        .RequireAuthorization("SuperAdmin")
        .RequirePermission("AdminResetPassword")
        .WithSummary("SuperAdmin resets any user's password");
    }
}