using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

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
            try
            {
                var subscription = await service.GetByIdAsync(id);
                return subscription != null ? Results.Ok(subscription) : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to retrieve subscription");
            }
        })
        .RequirePermission("Subscriptions.ViewAll")
        .WithSummary("Get subscription by ID");

        // ============================================
        // GET ACTIVE SUBSCRIPTION FOR CLINIC
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/active", async (
            Guid clinicId,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            try
            {
                // Security check
                if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                    return Results.Forbid();

                var subscriptions = await service.GetByClinicIdAsync(clinicId, false);
                var subscriptionsList = subscriptions.ToList();
                
                var activeSubscription = subscriptionsList
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
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to retrieve active subscription");
            }
        })
        .RequireAuthorization()
        .WithSummary("Get active subscription for clinic with expiry info");

        // ============================================
        // GET SUBSCRIPTIONS BY CLINIC
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
            Guid clinicId,
            [FromQuery] bool includePayments,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            try
            {
                // Security check
                if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                    return Results.Forbid();

                var subscriptions = await service.GetByClinicIdAsync(clinicId, includePayments);
                return Results.Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to retrieve subscriptions");
            }
        })
        .RequireAuthorization()
        .WithSummary("Get all subscriptions for a clinic");

        // ============================================
        // CREATE SUBSCRIPTION (SuperAdmin only)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<CreateSubscriptionRequestDto> validator) =>
        {
            try
            {
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

                var created = await service.CreateSubscriptionAsync(dto);
                return Results.Created($"/api/subscriptions/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to create subscription");
            }
        })
        .RequirePermission("Subscriptions.Create")
        .WithSummary("Create a new subscription (SuperAdmin only)");

        // ============================================
        // UPDATE SUBSCRIPTION
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<UpdateSubscriptionRequestDto> validator) =>
        {
            try
            {
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

                var updated = await service.UpdateSubscriptionAsync(id, dto);
                return updated != null ? Results.Ok(updated) : Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to update subscription");
            }
        })
        .RequirePermission("Subscriptions.Edit")
        .WithSummary("Update subscription");

        // ============================================
        // SOFT DELETE SUBSCRIPTION
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            try
            {
                var success = await service.SoftDeleteSubscriptionAsync(id);
                return success 
                    ? Results.Ok(new { Message = $"Subscription {id} soft deleted" }) 
                    : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to delete subscription");
            }
        })
        .RequirePermission("Subscriptions.Cancel")
        .WithSummary("Soft delete subscription");

        // ============================================
        // HARD DELETE SUBSCRIPTION
        // ============================================
        group.MapDelete("/{id:guid}/hard", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            try
            {
                var success = await service.DeleteSubscriptionAsync(id);
                return success 
                    ? Results.Ok(new { Message = $"Subscription {id} permanently deleted" }) 
                    : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Failed to delete subscription");
            }
        })
        .RequirePermission("Subscriptions.Cancel")
        .WithSummary("Permanently delete subscription (DANGEROUS)");
    }
}