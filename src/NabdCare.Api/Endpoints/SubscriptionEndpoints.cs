using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Domain.Constants;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions").WithTags("Subscriptions");

        // ============================================
        // 🆕 GET AVAILABLE PLANS (Product Catalog)
        // ============================================
        group.MapGet("/plans", () => Results.Ok(SubscriptionPlans.All))
            .RequireAuthorization() // Accessible to any authenticated user
            .WithName("GetSubscriptionPlans")
            .WithSummary("Get list of available subscription plans")
            .Produces<IEnumerable<PlanDefinition>>(StatusCodes.Status200OK);

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
        .WithAbac<Subscription>(Permissions.Subscriptions.View, "view", r => r as Subscription)
        .WithName("GetSubscriptionById")
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
                // Security Check
                if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                    return Results.Forbid();

                var active = await service.GetActiveSubscriptionAsync(clinicId);

                // 🛑 CHANGE HERE: Return 200 OK with null instead of 404 NotFound
                if (active == null)
                {
                    return Results.Ok<object?>(null);
                }

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
            .WithName("GetActiveSubscriptionForClinic")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 GET SUBSCRIPTIONS BY CLINIC (HISTORY)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
                Guid clinicId,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext,
                [AsParameters] PaginationRequestDto pagination,
                [FromQuery] bool includePayments = false) =>
            {
                if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                    return Results.Forbid();

                var result = await service.GetByClinicIdPagedAsync(clinicId, pagination, includePayments);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.View)
            .WithAbac<Subscription>(Permissions.Subscriptions.View, "list", r => r as Subscription)
            .WithName("GetSubscriptionsByClinic")
            .Produces<PaginatedResult<SubscriptionResponseDto>>(StatusCodes.Status200OK);

        // ============================================
        // 🔹 GET ALL SUBSCRIPTIONS (SuperAdmin)
        // ============================================
        group.MapGet("/", async (
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext, 
            [FromQuery] bool includePayments = false) =>
        {
            if (!tenantContext.IsSuperAdmin) return Results.Forbid();
            var result = await service.GetAllPagedAsync(pagination, includePayments);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.ViewAll)
        .WithName("GetAllSubscriptionsPaged")
        .Produces<PaginatedResult<SubscriptionResponseDto>>(StatusCodes.Status200OK);

        // ============================================
        // 🔹 CREATE SUBSCRIPTION
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<CreateSubscriptionRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            try 
            {
                var created = await service.CreateSubscriptionAsync(dto);
                return Results.Created($"/api/subscriptions/{created.Id}", created);
            }
            catch (ArgumentException ex) 
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.Create)
        .WithAbac<Subscription>(Permissions.Subscriptions.Create, "create", r => r as Subscription)
        .WithName("CreateSubscription")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

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
        .WithAbac(Permissions.Subscriptions.Edit, "edit", async ctx => 
        {
             var svc = ctx.RequestServices.GetRequiredService<ISubscriptionService>();
             var idStr = ctx.Request.RouteValues["id"]?.ToString();
             return Guid.TryParse(idStr, out var sid) ? await svc.GetByIdAsync(sid) : null;
        })
        .WithName("UpdateSubscription")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 CHANGE STATUS
        // ============================================
        group.MapPatch("/{id:guid}/status", async (
                Guid id,
                [FromBody] SubscriptionStatus newStatus,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin) return Results.Forbid();

                var updated = await service.UpdateSubscriptionStatusAsync(id, newStatus);
                return updated != null
                    ? Results.Ok(new { Message = $"Subscription {id} status changed to {newStatus}" })
                    : Results.BadRequest(new { Error = "Failed to update status (Subscription may not exist)" });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.ChangeStatus)
            .WithName("ChangeSubscriptionStatus")
            .Produces(StatusCodes.Status200OK);

        // ============================================
        // 🔹 RENEW (Admin / User?)
        // ============================================
        group.MapPost("/{id:guid}/renew", async (
                Guid id,
                [FromQuery] SubscriptionType type,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin) return Results.Forbid();

                try
                {
                    var renewed = await service.RenewSubscriptionAsync(id, type);
                    return Results.Ok(new { Message = "Renewed successfully", Subscription = renewed });
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { Error = "Subscription not found" });
                }
                catch (InvalidOperationException ex)
                {
                    // ✅ Handle "Already Queued" gracefully (Conflict)
                    return Results.Conflict(new { Error = ex.Message });
                }
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.Renew)
            .WithName("RenewSubscription")
            .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 TOGGLE AUTO-RENEW
        // ============================================
        group.MapPatch("/{id:guid}/auto-renew", async (
                Guid id,
                [FromQuery] bool enable,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin) return Results.Forbid();
                var updated = await service.ToggleAutoRenewAsync(id, enable);
                return updated != null ? Results.Ok(updated) : Results.NotFound();
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.ToggleAutoRenew)
            .WithName("ToggleAutoRenew");

        // ============================================
        // 🔹 CANCEL (Soft Delete Logic)
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id, 
            [FromServices] ISubscriptionService service) =>
        {
            // ✅ FIX: Use CancelSubscriptionAsync (we removed SoftDelete wrapper)
            var success = await service.CancelSubscriptionAsync(id);
            return success 
                ? Results.Ok(new { Message = "Subscription cancelled successfully." }) 
                : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.Cancel)
        .WithName("CancelSubscription")
        .WithSummary("Cancel a subscription (stops renewal, marks as cancelled)");

        // ============================================
        // 🔹 HARD DELETE (Admin Only)
        // ============================================
        group.MapDelete("/{id:guid}/hard", async (
            Guid id, 
            [FromServices] ISubscriptionService service, 
            [FromServices] ITenantContext ctx) =>
        {
            if (!ctx.IsSuperAdmin) return Results.Forbid();
            var success = await service.DeleteSubscriptionAsync(id);
            return success 
                ? Results.Ok(new { Message = "Subscription and all history permanently deleted." }) 
                : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.HardDelete)
        .WithName("HardDeleteSubscription");
    }
}