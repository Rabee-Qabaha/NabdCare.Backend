using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;

namespace NabdCare.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // ✅ Create User
        group.MapPost("/", async (
            [FromBody] CreateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var user = await userService.CreateUserAsync(dto);
                return Results.Created($"/api/users/{user.Id}", user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user with email {Email}", dto.Email);
                return Results.Problem("An error occurred while creating the user.");
            }
        })
        .RequirePermission("CreateUser")
        .WithSummary("Create a new user")
        .WithOpenApi();

        // ✅ Get User by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .RequirePermission("ViewUser")
        .WithSummary("Get user by ID")
        .WithOpenApi();

        // ✅ Get Users (by clinic if ClinicAdmin)
        group.MapGet("/", async (
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            var users = await userService.GetUsersByClinicIdAsync(null); // TenantContext handles filtering
            return Results.Ok(users);
        })
        .RequirePermission("ViewUsers")
        .WithSummary("List all users for current clinic or globally (if SuperAdmin)")
        .WithOpenApi();

        // ✅ Update User (non-role properties)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequestDto dto,
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            var updatedUser = await userService.UpdateUserAsync(id, dto);
            return updatedUser is not null
                ? Results.Ok(updatedUser)
                : Results.NotFound($"User with ID {id} not found.");
        })
        .RequirePermission("UpdateUser")
        .WithSummary("Update user details")
        .WithOpenApi();

        // ✅ Update User Role
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto request,
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            var updatedUser = await userService.UpdateUserRoleAsync(id, request.Role);
            return updatedUser is not null
                ? Results.Ok(updatedUser)
                : Results.NotFound($"User with ID {id} not found.");
        })
        .RequirePermission("UpdateUserRole")
        .WithSummary("Update user's role")
        .WithOpenApi();

        // ✅ Soft Delete User
        group.MapDelete("/{id:guid}/soft", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            var success = await userService.SoftDeleteUserAsync(id);
            return success ? Results.NoContent() : Results.NotFound($"User with ID {id} not found.");
        })
        .RequirePermission("DeleteUser")
        .WithSummary("Soft delete user")
        .WithOpenApi();

        // ✅ Hard Delete User
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ILogger<Program> logger) =>
        {
            var success = await userService.DeleteUserAsync(id);
            return success ? Results.NoContent() : Results.NotFound($"User with ID {id} not found.");
        })
        .RequirePermission("DeleteUser")
        .WithSummary("Permanently delete user")
        .WithOpenApi();
    }
}