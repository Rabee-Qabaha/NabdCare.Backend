using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Api.Endpoints;

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
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin && dto.ClinicId != tenantContext.ClinicId)
                return Results.Forbid();

            var user = await userService.CreateUserAsync(dto);
            return Results.Created($"/api/users/{user.Id}", user);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Create)
        .WithAbac(
            Permissions.Users.Create,
            "create",
            async ctx =>
            {
                var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();
                return await Task.FromResult(tenant.IsSuperAdmin
                    ? null
                    : new User { ClinicId = tenant.ClinicId });
            })
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .WithDescription("SuperAdmin: any clinic. ClinicAdmin: own clinic only.")
        .Produces<UserResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status409Conflict);

        // ============================================
        // GET PAGED USERS (ABAC + Multi-Tenant + Soft-Delete Toggle)
        // ============================================
        group.MapGet("/paged", async (
            [AsParameters] PaginationRequestDto request,
            [FromQuery] Guid? clinicId,
            [FromQuery] bool includeDeleted,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (request.Limit <= 0)
                return Results.BadRequest(new { Error = "Limit must be greater than 0" });

            PaginatedResult<UserResponseDto> result;

            if (tenantContext.IsSuperAdmin)
            {
                result = clinicId.HasValue
                    ? await userService.GetByClinicIdPagedAsync(clinicId.Value, request.Limit, request.Cursor, includeDeleted)
                    : await userService.GetAllPagedAsync(request.Limit, request.Cursor, includeDeleted);
            }
            else if (tenantContext.ClinicId.HasValue)
            {
                result = await userService.GetByClinicIdPagedAsync(
                    tenantContext.ClinicId.Value,
                    request.Limit,
                    request.Cursor,
                    includeDeleted
                );
            }
            else
            {
                return Results.Json(
                    new { Error = "You don't have permission to view users" },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.View)
        .WithAbac<User>(
            Permissions.Users.View,
            "list",
            async ctx =>
            {
                var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();
                return await Task.FromResult(tenant.IsSuperAdmin
                    ? null
                    : new User { ClinicId = tenant.ClinicId });
            })
        .WithName("GetPagedUsers")
        .WithSummary("Get paged users (multi-tenant + ABAC enforced)")
        .Produces<PaginatedResult<UserResponseDto>>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET USERS BY CLINIC (ABAC + Multi-Tenant + Soft-Delete Toggle)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/paged", async (
            Guid clinicId,
            [AsParameters] PaginationRequestDto request,
            [FromQuery] bool includeDeleted,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (clinicId == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            if (!tenantContext.IsSuperAdmin)
            {
                if (!tenantContext.ClinicId.HasValue)
                    return Results.Forbid();

                clinicId = tenantContext.ClinicId.Value;
            }

            var result = await userService.GetByClinicIdPagedAsync(
                clinicId,
                request.Limit,
                request.Cursor,
                includeDeleted
            );

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.View)
        .WithAbac<User>(
            Permissions.Users.View,
            "list",
            async ctx =>
            {
                var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();
                return await Task.FromResult(
                    tenant.IsSuperAdmin
                        ? null
                        : new User { ClinicId = tenant.ClinicId }
                );
            })
        .WithName("GetUsersByClinicPaged")
        .WithSummary("Get users in a clinic (paged, with soft-delete & ABAC)")
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
            var user = await userService.GetUserByIdAsync(id);
            return user is not null
                ? Results.Ok(user)
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.ViewDetails)
        .WithAbac(
            Permissions.Users.ViewDetails,
            "view",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await svc.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("GetUserById");

        // ============================================
        // GET CURRENT USER (Me)
        // ============================================
        group.MapGet("/me", async (
            [FromServices] IUserService userService,
            [FromServices] IUserContext userContext) =>
        {
            var userIdStr = userContext.GetCurrentUserId();
            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var user = await userService.GetUserByIdAsync(userId);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser");

        // ============================================
        // CHECK EMAIL STATUS (Exists + Deleted State)
        // ============================================
        group.MapGet("/check-email", async (
            [FromQuery] string email,
            [FromServices] IUserService userService) =>
        {
            if (string.IsNullOrWhiteSpace(email))
                return Results.BadRequest(new { Error = "Email is required" });

            var (exists, isDeleted, userId) = await userService.EmailExistsDetailedAsync(email);

            return Results.Ok(new
            {
                Exists = exists,
                IsDeleted = isDeleted,
                UserId = userId
            });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Create)
        .WithName("CheckEmailStatus")
        .WithSummary("Check if an email exists and whether that user is soft-deleted.");

        // ============================================
        // RESTORE USER (ABAC + Multi-Tenant)
        // ============================================
        group.MapPut("/{id:guid}/restore", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            var restored = await userService.RestoreUserAsync(id);

            return restored is not null
                ? Results.Ok(restored)
                : Results.NotFound(new { Error = $"User {id} not found or not deleted" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Restore)
        .WithAbac(
            Permissions.Users.Restore,
            "restore",
            async ctx =>
            {
                var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();
                var svc = ctx.RequestServices.GetRequiredService<IUserService>();

                var idStr = ctx.Request.RouteValues["id"]?.ToString();

                if (!Guid.TryParse(idStr, out var userId))
                    return null;

                var user = await svc.GetUserByIdAsync(userId);

                return tenant.IsSuperAdmin
                    ? null
                    : new User { ClinicId = tenant.ClinicId };
            })
        .WithName("RestoreUser")
        .WithSummary("Restore a soft-deleted user")
        .WithDescription("SuperAdmin: restore any user. ClinicAdmin: restore users only from their own clinic.")
        .Produces<UserResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // UPDATE USER
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.UpdateUserAsync(id, dto);
            return updatedUser is not null
                ? Results.Ok(updatedUser)
                : Results.NotFound(new { Error = $"User {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Edit)
        .WithAbac(
            Permissions.Users.Edit,
            "edit",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await svc.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("UpdateUser");

        // ============================================
        // UPDATE USER ROLE
        // ============================================
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            [FromServices] IUserService userService) =>
        {
            var updatedUser = await userService.UpdateUserRoleAsync(id, dto.RoleId);
            return updatedUser is not null ? Results.Ok(updatedUser) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.ChangeRole)
        .WithAbac(
            Permissions.Users.ChangeRole,
            "changeRole",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await svc.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("UpdateUserRole");

        // ============================================
        // ACTIVATE / DEACTIVATE
        // ============================================
        group.MapPut("/{id:guid}/activate", async (Guid id, [FromServices] IUserService svc) =>
        {
            var u = await svc.ActivateUserAsync(id);
            return u is not null ? Results.Ok(u) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Activate)
        .WithAbac(
            Permissions.Users.Activate,
            "activate",
            async ctx =>
            {
                var s = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await s.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("ActivateUser");

        group.MapPut("/{id:guid}/deactivate", async (Guid id, [FromServices] IUserService svc) =>
        {
            var u = await svc.DeactivateUserAsync(id);
            return u is not null ? Results.Ok(u) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Activate)
        .WithAbac(
            Permissions.Users.Activate,
            "deactivate",
            async ctx =>
            {
                var s = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await s.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("DeactivateUser");

        // ============================================
        // SOFT / HARD DELETE
        // ============================================
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IUserService svc) =>
        {
            var ok = await svc.SoftDeleteUserAsync(id);
            return ok ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.Delete)
        .WithAbac(
            Permissions.Users.Delete,
            "delete",
            async ctx =>
            {
                var s = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await s.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("DeleteUser");

        group.MapDelete("/{id:guid}/permanent", async (
            Guid id,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var success = await userService.HardDeleteUserAsync(id);
            return success ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.HardDelete)
        .WithName("HardDeleteUser");

        // ============================================
        // PASSWORD OPERATIONS
        // ============================================
        group.MapPost("/{id:guid}/change-password", async (
            Guid id,
            [FromBody] ChangePasswordRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updated = await userService.ChangePasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .WithName("ChangePassword");

        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            [FromBody] ResetPasswordRequestDto dto,
            [FromServices] IUserService userService) =>
        {
            var updated = await userService.ResetPasswordAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.ResetPassword)
        .WithAbac(
            Permissions.Users.ResetPassword,
            "resetPassword",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IUserService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var uid)
                    ? await svc.GetUserByIdAsync(uid)
                    : null;
            })
        .WithName("ResetPassword");
    }
}