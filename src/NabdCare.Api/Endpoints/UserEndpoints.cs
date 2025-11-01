using FluentValidation;
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
            [FromServices] ITenantContext tenantContext,
            [FromServices] IValidator<CreateUserRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            // ✅ ABAC check: ClinicAdmin can only create in their own clinic
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
        // GET PAGED USERS (ABAC + Multi-Tenant)
        // ============================================
        group.MapGet("/paged", async (
            [AsParameters] PaginationRequestDto request,
            [FromQuery] Guid? clinicId,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (request.Limit <= 0)
                return Results.BadRequest(new { Error = "Limit must be greater than 0" });

            PaginatedResult<UserResponseDto> result;

            if (tenantContext.IsSuperAdmin)
            {
                result = clinicId.HasValue
                    ? await userService.GetByClinicIdPagedAsync(clinicId.Value, request.Limit, request.Cursor)
                    : await userService.GetAllPagedAsync(request.Limit, request.Cursor);
            }
            else if (tenantContext.ClinicId.HasValue)
            {
                result = await userService.GetByClinicIdPagedAsync(
                    tenantContext.ClinicId.Value,
                    request.Limit,
                    request.Cursor
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
        // GET USERS BY CLINIC (SuperAdmin Only)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/paged", async (
            Guid clinicId,
            [AsParameters] PaginationRequestDto request,
            [FromServices] IUserService userService,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (clinicId == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var result = await userService.GetByClinicIdPagedAsync(clinicId, request.Limit, request.Cursor);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Users.ViewAll)
        .WithAbac<User>(
            Permissions.Users.ViewAll,
            "list",
            async ctx =>
            {
                var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();
                return await Task.FromResult(tenant.IsSuperAdmin
                    ? null
                    : new User { ClinicId = tenant.ClinicId });
            })
        .WithName("GetUsersByClinicPaged")
        .WithSummary("Get users in a specific clinic (paged)")
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
        // UPDATE USER
        // ============================================
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
            });

        // ============================================
        // UPDATE USER ROLE
        // ============================================
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            [FromBody] UpdateUserRoleDto dto,
            [FromServices] IUserService userService,
            [FromServices] IValidator<UpdateUserRoleDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

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
            });

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
            });

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
            });

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
            });

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
        .RequirePermission(Permissions.Users.HardDelete);

        // ============================================
        // PASSWORD OPERATIONS
        // ============================================
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
        .RequireAuthorization();

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
            });
    }
}