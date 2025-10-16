using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;

namespace NabdCare.Api.Endpoints;

public static class ClinicEndpoints
{
    public static void MapClinicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clinics").WithTags("Clinics");

        // Create Clinic
        group.MapPost("/", async (
            [FromBody] CreateClinicRequestDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<CreateClinicRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var created = await service.CreateClinicAsync(dto);
            return Results.Created($"/api/clinics/{created.Id}", created);
        })
        .RequireAuthorization()
        .WithSummary("Create a new clinic");

        // Get by Id
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            var clinic = await service.GetClinicByIdAsync(id);
            return clinic != null ? Results.Ok(clinic) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Get clinic by ID");

        // Get all clinics
        group.MapGet("/", async (
            [FromServices] IClinicService service) =>
        {
            var clinics = await service.GetAllClinicsAsync();
            return Results.Ok(clinics);
        })
        .RequireAuthorization()
        .WithSummary("Get all clinics");

        // Get paged clinics
        group.MapGet("/paged", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromServices] IClinicService service) =>
        {
            var clinics = await service.GetPagedClinicsAsync(page, pageSize);
            return Results.Ok(clinics);
        })
        .RequireAuthorization()
        .WithSummary("Get clinics paged");

        // Update Clinic
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateClinicRequestDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<UpdateClinicRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await service.UpdateClinicAsync(id, dto);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Update clinic");

        // Soft delete
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            var success = await service.SoftDeleteClinicAsync(id);
            return success ? Results.Ok($"Clinic {id} soft deleted.") : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Soft delete clinic");

        // Hard delete
        group.MapDelete("/{id:guid}/hard", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            var success = await service.DeleteClinicAsync(id);
            return success ? Results.Ok($"Clinic {id} permanently deleted.") : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Permanently delete clinic");
    }
}