using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Subscription management endpoints (SuperAdmin and ClinicAdmin)
/// </summary>
public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions").WithTags("Subscriptions");

        // ============================================
        // GET SUBSCRIPTION BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var subscription = await service.GetByIdAsync(id);
            return subscription != null ? Results.Ok(subscription) : Results.NotFound(new { Error = $"Subscription {id} not found" });
        })
        .RequirePermission("Subscriptions.ViewAll")
        .WithSummary("Get subscription by ID")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET ACTIVE SUBSCRIPTION FOR CLINIC
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/active", async (
            Guid clinicId,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                return Results.Forbid();

            var result = await service.GetByClinicIdPagedAsync(clinicId, new PaginationRequestDto { Limit = 50 });
            var activeSubscription = result.Items
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (activeSubscription == null)
                return Results.NotFound(new { Error = "No active subscription found for this clinic" });

            var daysRemaining = (activeSubscription.EndDate - DateTime.UtcNow).Days;

            return Results.Ok(new
            {
                Subscription = activeSubscription,
                DaysRemaining = daysRemaining,
                IsExpiringSoon = daysRemaining <= 30 && daysRemaining > 0,
                IsExpired = daysRemaining < 0
            });
        })
        .RequireAuthorization()
        .WithSummary("Get active subscription for clinic with expiry info")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET SUBSCRIPTIONS BY CLINIC (Paginated)
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
        .WithSummary("Get paginated subscriptions for a clinic")
        .Produces<PaginatedResult<SubscriptionResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET ALL SUBSCRIPTIONS (SuperAdmin only, Paginated)
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
        .RequirePermission("Subscriptions.ViewAll")
        .WithSummary("Get all subscriptions (SuperAdmin only, paginated)")
        .Produces<PaginatedResult<SubscriptionResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // CREATE SUBSCRIPTION (SuperAdmin only)
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
        .RequirePermission("Subscriptions.Create")
        .WithSummary("Create a new subscription (SuperAdmin only)")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // UPDATE SUBSCRIPTION
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
        .RequirePermission("Subscriptions.Edit")
        .WithSummary("Update subscription")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // SOFT DELETE SUBSCRIPTION
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var success = await service.SoftDeleteSubscriptionAsync(id);
            return success
                ? Results.Ok(new { Message = $"Subscription {id} soft deleted successfully" })
                : Results.NotFound(new { Error = $"Subscription {id} not found" });
        })
        .RequirePermission("Subscriptions.Cancel")
        .WithSummary("Soft delete subscription")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // HARD DELETE SUBSCRIPTION (SuperAdmin Only)
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
        .RequirePermission("Subscriptions.Cancel")
        .WithSummary("Permanently delete subscription (SuperAdmin only)")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}