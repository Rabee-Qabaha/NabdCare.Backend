using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;

namespace NabdCare.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        // Get subscription by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var subscription = await service.GetByIdAsync(id);
            return subscription != null ? Results.Ok(subscription) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Get subscription by ID");

        // Get subscriptions by clinic
        group.MapGet("/clinic/{clinicId:guid}", async (
            Guid clinicId,
            [FromQuery] bool includePayments,
            [FromServices] ISubscriptionService service) =>
        {
            var subscriptions = await service.GetByClinicIdAsync(clinicId, includePayments);
            return Results.Ok(subscriptions);
        })
        .RequireAuthorization()
        .WithSummary("Get all subscriptions for a clinic");

        // Get all subscriptions paged
        group.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] bool includePayments,
            [FromServices] ISubscriptionService service) =>
        {
            var subscriptions = await service.GetPagedAsync(page, pageSize, includePayments);
            return Results.Ok(subscriptions);
        })
        .RequireAuthorization()
        .WithSummary("Get all subscriptions paged");

        // Create subscription
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
        .WithSummary("Create a new subscription");

        // Update subscription
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
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Update subscription");

        // Soft delete subscription
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var success = await service.SoftDeleteSubscriptionAsync(id);
            return success ? Results.Ok($"Subscription {id} soft deleted.") : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Soft delete subscription");

        // Hard delete subscription
        group.MapDelete("/{id:guid}/hard", async (
            Guid id,
            [FromServices] ISubscriptionService service) =>
        {
            var success = await service.DeleteSubscriptionAsync(id);
            return success ? Results.Ok($"Subscription {id} permanently deleted.") : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Permanently delete subscription");
    }
}
