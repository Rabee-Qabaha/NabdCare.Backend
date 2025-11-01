using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Subscription management endpoints (SuperAdmin and ClinicAdmin)
/// Fully secured with RBAC + ABAC + Authorization middleware.
/// </summary>
public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions").WithTags("Subscriptions");

        // ============================================
        // 🔹 GET SUBSCRIPTION BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var subscription = await service.GetByIdAsync(id);
            return subscription != null
                ? Results.Ok(subscription)
                : Results.NotFound(new { Error = $"Subscription {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.View)
        .WithAbac<Subscription>(
            Permissions.Subscriptions.View,
            "view",
            r => r as Subscription)
        .WithName("GetSubscriptionById")
        .WithSummary("Get subscription by ID")
        .WithDescription("SuperAdmin: any subscription. ClinicAdmin: only their own clinic's subscriptions.")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 GET ACTIVE SUBSCRIPTION FOR CLINIC
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/active", async (
            Guid clinicId,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                return Results.Forbid();

            var result = await service.GetByClinicIdPagedAsync(clinicId, new PaginationRequestDto { Limit = 50 });
            var active = result.Items
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (active == null)
                return Results.NotFound(new { Error = "No active subscription found for this clinic" });

            var daysRemaining = (active.EndDate - DateTime.UtcNow).Days;

            return Results.Ok(new
            {
                Subscription = active,
                DaysRemaining = daysRemaining,
                IsExpiringSoon = daysRemaining <= 30 && daysRemaining > 0,
                IsExpired = daysRemaining < 0
            });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.ViewActive)
        .WithAbac<Subscription>(
            Permissions.Subscriptions.ViewActive,
            "viewActive",
            r => r as Subscription)
        .WithName("GetActiveSubscriptionForClinic")
        .WithSummary("Get active subscription for a clinic")
        .WithDescription("SuperAdmin: any clinic. ClinicAdmin: only their own clinic.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 GET SUBSCRIPTIONS BY CLINIC (Paginated)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
            Guid clinicId,
            [AsParameters] PaginationRequestDto pagination,
            [FromQuery] bool includePayments,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                return Results.Forbid();

            var result = await service.GetByClinicIdPagedAsync(clinicId, pagination, includePayments);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.View)
        .WithAbac<Subscription>(
            Permissions.Subscriptions.View,
            "list",
            r => r as Subscription)
        .WithName("GetSubscriptionsByClinic")
        .WithSummary("Get paginated subscriptions for a clinic")
        .WithDescription("SuperAdmin: any clinic. ClinicAdmin: own clinic only.")
        .Produces<PaginatedResult<SubscriptionResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 GET ALL SUBSCRIPTIONS (SuperAdmin only)
        // ============================================
        group.MapGet("/", async (
            [AsParameters] PaginationRequestDto pagination,
            [FromQuery] bool includePayments,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var result = await service.GetAllPagedAsync(pagination, includePayments);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.ViewAll)
        .WithAbac<Subscription>(
            Permissions.Subscriptions.ViewAll,
            "viewAll",
            r => r as Subscription)
        .WithName("GetAllSubscriptionsPaged")
        .WithSummary("Get all subscriptions (SuperAdmin only, paginated)")
        .WithDescription("SuperAdmin only.")
        .Produces<PaginatedResult<SubscriptionResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 CREATE SUBSCRIPTION (SuperAdmin only)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<CreateSubscriptionRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var created = await service.CreateSubscriptionAsync(dto);
            return Results.Created($"/api/subscriptions/{created.Id}", created);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.Create)
        .WithAbac<Subscription>(
            Permissions.Subscriptions.Create,
            "create",
            r => r as Subscription)
        .WithName("CreateSubscription")
        .WithSummary("Create a new subscription (SuperAdmin only)")
        .WithDescription("Creates a new subscription record for a clinic.")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 UPDATE SUBSCRIPTION
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<UpdateSubscriptionRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await service.UpdateSubscriptionAsync(id, dto);
            return updated != null
                ? Results.Ok(updated)
                : Results.NotFound(new { Error = $"Subscription {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.Edit)
        .WithAbac(
            Permissions.Subscriptions.Edit,
            "edit",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var sid)
                    ? await svc.GetByIdAsync(sid)
                    : null;
            })
        .WithName("UpdateSubscription")
        .WithSummary("Update an existing subscription")
        .WithDescription("SuperAdmin: update any subscription. ClinicAdmin: update own clinic's subscriptions.")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 CHANGE SUBSCRIPTION STATUS
        // ============================================
        group.MapPatch("/{id:guid}/status", async (
                Guid id,
                [FromBody] SubscriptionStatus newStatus,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin)
                    return Results.Forbid();

                var subscription = await service.GetByIdAsync(id);
                if (subscription == null)
                    return Results.NotFound(new { Error = $"Subscription {id} not found" });

                if (subscription.Status == SubscriptionStatus.Cancelled && newStatus == SubscriptionStatus.Active)
                    return Results.BadRequest(new { Error = "Cannot reactivate a cancelled subscription." });

                var updated = await service.UpdateSubscriptionStatusAsync(id, newStatus);
                return updated != null
                    ? Results.Ok(new { Message = $"Subscription {id} status changed to {newStatus}" })
                    : Results.BadRequest(new { Error = "Failed to update subscription status" });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.ChangeStatus)
            .WithAbac(
                Permissions.Subscriptions.ChangeStatus,
                "changeStatus",
                async ctx =>
                {
                    var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
                    var idStr = ctx.Request.RouteValues["id"]?.ToString();
                    return Guid.TryParse(idStr, out var sid)
                        ? await svc.GetByIdAsync(sid)
                        : null;
                })
            .WithName("ChangeSubscriptionStatus")
            .WithSummary("Change subscription status (SuperAdmin only)")
            .WithDescription("Allows SuperAdmin to update the subscription status to Active, Suspended, etc.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 RENEW SUBSCRIPTION (SuperAdmin only)
        // ============================================
        group.MapPost("/{id:guid}/renew", async (
                Guid id,
                [FromQuery] SubscriptionType type,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin)
                    return Results.Forbid();

                var subscription = await service.GetByIdAsync(id);
                if (subscription == null)
                    return Results.NotFound(new { Error = $"Subscription {id} not found" });

                var renewed = await service.RenewSubscriptionAsync(id, type);
                return Results.Ok(new
                {
                    Message = $"Subscription {id} renewed successfully.",
                    NewSubscriptionId = renewed.Id,
                    renewed
                });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.Renew)
            .WithAbac(
                Permissions.Subscriptions.Renew,
                "renew",
                async ctx =>
                {
                    var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
                    var idStr = ctx.Request.RouteValues["id"]?.ToString();
                    return Guid.TryParse(idStr, out var sid)
                        ? await svc.GetByIdAsync(sid)
                        : null;
                })
            .WithName("RenewSubscription")
            .WithSummary("Renew an existing subscription (SuperAdmin only)")
            .WithDescription("Creates a new subscription for the same clinic, extending service access.")
            .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 TOGGLE AUTO-RENEW STATUS (SuperAdmin only)
        // ============================================
        group.MapPatch("/{id:guid}/auto-renew", async (
                Guid id,
                [FromQuery] bool enable,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin)
                    return Results.Forbid();

                var updated = await service.ToggleAutoRenewAsync(id, enable);
                return updated != null
                    ? Results.Ok(new
                    {
                        Message = $"Auto-renew {(enable ? "enabled" : "disabled")} for subscription {id}",
                        updated
                    })
                    : Results.NotFound(new { Error = $"Subscription {id} not found" });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.ToggleAutoRenew)
            .WithAbac(
                Permissions.Subscriptions.ToggleAutoRenew,
                "toggleAutoRenew",
                async ctx =>
                {
                    var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
                    var idStr = ctx.Request.RouteValues["id"]?.ToString();
                    return Guid.TryParse(idStr, out var sid)
                        ? await svc.GetByIdAsync(sid)
                        : null;
                })
            .WithName("ToggleAutoRenew")
            .WithSummary("Enable or disable Auto-Renew for a subscription (SuperAdmin only)")
            .WithDescription("Toggles the automatic renewal flag for a subscription record.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 CANCEL (SOFT DELETE) SUBSCRIPTION
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var success = await service.SoftDeleteSubscriptionAsync(id);
            return success
                ? Results.Ok(new { Message = $"Subscription {id} canceled (soft deleted)" })
                : Results.NotFound(new { Error = $"Subscription {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.Delete)
        .WithAbac(
            Permissions.Subscriptions.Delete,
            "cancel",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var sid)
                    ? await svc.GetByIdAsync(sid)
                    : null;
            })
        .WithName("CancelSubscription")
        .WithSummary("Cancel (soft delete) a subscription")
        .WithDescription("Marks a subscription as canceled without permanent removal.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 HARD DELETE (PERMANENT) SUBSCRIPTION
        // ============================================
        group.MapDelete("/{id:guid}/hard", async (
            Guid id,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var success = await service.DeleteSubscriptionAsync(id);
            return success
                ? Results.Ok(new { Message = $"Subscription {id} permanently deleted" })
                : Results.NotFound(new { Error = $"Subscription {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.HardDelete)
        .WithAbac(
            Permissions.Subscriptions.HardDelete,
            "delete",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var sid)
                    ? await svc.GetByIdAsync(sid)
                    : null;
            })
        .WithName("HardDeleteSubscription")
        .WithSummary("Permanently delete a subscription (SuperAdmin only)")
        .WithDescription("⚠️ Permanently removes the subscription from the database.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}