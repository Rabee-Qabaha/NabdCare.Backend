using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Clinic management endpoints with multi-tenant security.
/// No try-catch blocks - exceptions bubble up to middleware.
/// </summary>
public static class ClinicEndpoints
{
    public static void MapClinicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clinics")
            .WithTags("Clinics");

        // ============================================
        // CREATE CLINIC (SuperAdmin Only)
        // ============================================
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
        .RequirePermission("Clinics.Create")
        .WithName("CreateClinic")
        .WithSummary("Create a new clinic (SuperAdmin only)")
        .WithDescription("Creates a new clinic with subscription and initial configuration.")
        .Produces<ClinicResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET CURRENT USER'S CLINIC
        // ============================================
        group.MapGet("/me", async (
            [FromServices] IClinicService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (tenantContext.IsSuperAdmin)
                return Results.BadRequest(new { Error = "SuperAdmin does not belong to a clinic" });

            if (!tenantContext.ClinicId.HasValue)
                return Results.BadRequest(new { Error = "You don't belong to any clinic" });

            var clinic = await service.GetClinicByIdAsync(tenantContext.ClinicId.Value);
            
            return clinic != null 
                ? Results.Ok(clinic) 
                : Results.NotFound(new { Error = "Your clinic was not found" });
        })
        .RequireAuthorization()
        .WithName("GetMyClinic")
        .WithSummary("Get current user's clinic")
        .WithDescription("Returns the clinic that the authenticated user belongs to.")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET CLINIC BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IClinicService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var clinic = await service.GetClinicByIdAsync(id);
            if (clinic == null)
                return Results.NotFound(new { Error = $"Clinic {id} not found" });

            // SuperAdmin can view all clinics
            if (tenantContext.IsSuperAdmin)
                return Results.Ok(clinic);

            // Clinic users can only view their own clinic
            if (tenantContext.ClinicId.HasValue && tenantContext.ClinicId == id)
                return Results.Ok(clinic);

            return Results.Forbid();
        })
        .RequireAuthorization()
        .WithName("GetClinicById")
        .WithSummary("Get clinic by ID")
        .WithDescription("SuperAdmin: View any clinic. Clinic users: View own clinic only.")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET ALL CLINICS (SuperAdmin Only)
        // ============================================
        group.MapGet("/", async (
            [FromServices] IClinicService service) =>
        {
            var clinics = await service.GetAllClinicsAsync();
            return Results.Ok(clinics);
        })
        .RequireAuthorization()
        .WithName("GetAllClinics")
        .WithSummary("Get all clinics (SuperAdmin only)")
        .WithDescription("Returns a list of all clinics in the system.")
        .Produces<IEnumerable<ClinicResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET ACTIVE CLINICS (SuperAdmin Only)
        // ============================================
        group.MapGet("/active", async (
            [FromServices] IClinicService service) =>
        {
            var clinics = await service.GetActiveClinicsAsync();
            return Results.Ok(clinics);
        })
        .RequireAuthorization()
        .WithName("GetActiveClinics")
        .WithSummary("Get all active clinics (SuperAdmin only)")
        .WithDescription("Returns only clinics with Active status.")
        .Produces<IEnumerable<ClinicResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // SEARCH CLINICS (SuperAdmin Only)
        // ============================================
        group.MapGet("/search", async (
            [FromQuery] string? query,
            [FromServices] IClinicService service) =>
        {
            if (string.IsNullOrWhiteSpace(query))
                return Results.BadRequest(new { Error = "Search query is required" });

            var clinics = await service.SearchClinicsAsync(query);
            return Results.Ok(clinics);
        })
        .RequirePermission("Clinics.ViewAll")
        .WithName("SearchClinics")
        .WithSummary("Search clinics by name, email, or phone (SuperAdmin only)")
        .WithDescription("Searches clinics using name, email, or phone number.")
        .Produces<IEnumerable<ClinicResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // UPDATE CLINIC
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateClinicRequestDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<UpdateClinicRequestDto> validator,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            // SuperAdmin can update any clinic, ClinicAdmin can only update own clinic
            if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != id)
                return Results.Forbid();

            var updated = await service.UpdateClinicAsync(id, dto);
            
            return updated != null 
                ? Results.Ok(updated) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.Edit")
        .WithName("UpdateClinic")
        .WithSummary("Update clinic details")
        .WithDescription("SuperAdmin: Update any clinic. ClinicAdmin: Update own clinic only.")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // UPDATE CLINIC STATUS (SuperAdmin Only)
        // ============================================
        group.MapPut("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateClinicStatusDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<UpdateClinicStatusDto> validator) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var updated = await service.UpdateClinicStatusAsync(id, dto);
            
            return updated != null 
                ? Results.Ok(updated) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.ManageStatus")
        .WithName("UpdateClinicStatus")
        .WithSummary("Update clinic status (SuperAdmin only)")
        .WithDescription("Change clinic status (Active, Suspended, Inactive, Trial).")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // ACTIVATE CLINIC (SuperAdmin Only)
        // ============================================
        group.MapPut("/{id:guid}/activate", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var activated = await service.ActivateClinicAsync(id);
            
            return activated != null 
                ? Results.Ok(new { Message = $"Clinic {id} activated successfully", Clinic = activated }) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.ManageStatus")
        .WithName("ActivateClinic")
        .WithSummary("Activate a clinic (SuperAdmin only)")
        .WithDescription("Sets clinic status to Active.")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // SUSPEND CLINIC (SuperAdmin Only)
        // ============================================
        group.MapPut("/{id:guid}/suspend", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var suspended = await service.SuspendClinicAsync(id);
            
            return suspended != null 
                ? Results.Ok(new { Message = $"Clinic {id} suspended successfully", Clinic = suspended }) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.ManageStatus")
        .WithName("SuspendClinic")
        .WithSummary("Suspend a clinic (SuperAdmin only)")
        .WithDescription("Sets clinic status to Suspended. Users cannot login.")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // SOFT DELETE CLINIC (SuperAdmin Only)
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var success = await service.SoftDeleteClinicAsync(id);
            
            return success 
                ? Results.Ok(new { Message = $"Clinic {id} soft deleted successfully" }) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.Delete")
        .WithName("SoftDeleteClinic")
        .WithSummary("Soft delete a clinic (SuperAdmin only)")
        .WithDescription("Marks clinic as deleted. Can be restored. Users cannot login.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // HARD DELETE CLINIC (SuperAdmin Only - DANGEROUS)
        // ============================================
        group.MapDelete("/{id:guid}/permanent", async (
            Guid id,
            [FromServices] IClinicService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            // Only SuperAdmin can hard delete
            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var success = await service.DeleteClinicAsync(id);
            
            return success 
                ? Results.Ok(new { Message = $"Clinic {id} permanently deleted" }) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.HardDelete")
        .WithName("HardDeleteClinic")
        .WithSummary("Permanently delete a clinic (SuperAdmin only - IRREVERSIBLE)")
        .WithDescription("⚠️ DANGEROUS: Completely removes clinic and all related data from database.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET CLINIC STATISTICS (SuperAdmin Only)
        // ============================================
        group.MapGet("/{id:guid}/stats", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var stats = await service.GetClinicStatisticsAsync(id);
            
            return stats != null 
                ? Results.Ok(stats) 
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequirePermission("Clinics.ViewStats")
        .WithName("GetClinicStatistics")
        .WithSummary("Get clinic statistics (SuperAdmin only)")
        .WithDescription("Returns user count, subscription info, and other statistics.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}