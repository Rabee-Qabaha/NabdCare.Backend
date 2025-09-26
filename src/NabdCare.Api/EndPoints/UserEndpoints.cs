using FluentValidation;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Api.Endpoints;
public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // Create User
        app.MapPost("/api/users", async (User user, IUserService userService, IValidator<User> validator) =>
        {
            var validationResult = await validator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new
                {
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage)
                });
            }

            var createdUser = await userService.CreateUserAsync(user);
            return Results.Created($"/api/users/{createdUser.Id}", createdUser);
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithTags("Users")
        .WithSummary("Create a new user");

        // Get User by ID
        app.MapGet("/api/users/{id:guid}", async (Guid id, IUserService userService) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization()
        .WithTags("Users")
        .WithSummary("Get user by ID");

        // Get All Users
        app.MapGet("/api/users", async (Guid? clinicId, IUserService userService) =>
        {
            var users = await userService.GetUsersByClinicIdAsync(clinicId);
            return Results.Ok(users);
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithTags("Users")
        .WithSummary("Get all users");

        // Update User
        app.MapPut("/api/users/{id:guid}", async (Guid id, User user, IUserService userService) =>
        {
            if (id != user.Id)
            {
                return Results.BadRequest("User ID mismatch.");
            }

            var updatedUser = await userService.UpdateUserAsync(user);
            return Results.Ok(updatedUser);
        })
        .RequireAuthorization("SuperAdmin", "ClinicAdmin")
        .WithTags("Users")
        .WithSummary("Update an existing user");

        // Soft Delete User
        app.MapDelete("/api/users/{id:guid}", async (Guid id, IUserService userService) =>
        {
            var success = await userService.SoftDeleteUserAsync(id);
            return success ? Results.Ok($"User with ID {id} has been deleted.") : Results.NotFound($"User with ID {id} not found.");
        })
        .RequireAuthorization("SuperAdmin")
        .WithTags("Users")
        .WithSummary("Soft delete a user");
    }
}