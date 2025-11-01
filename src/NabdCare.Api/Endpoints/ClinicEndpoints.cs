using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Clinic management endpoints with multi-tenant security and ABAC enforcement.
/// Author: Rabee Qabaha
/// Updated: 2025-10-31 23:10 UTC
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
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Create)
        .WithAbac<Clinic>(Permissions.Clinics.Create, "create", r => r as Clinic)
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
        .RequirePermission(Permissions.Clinic.View)
        .WithAbac<Clinic>(Permissions.Clinic.View, "view", r => r as Clinic)
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

            if (tenantContext.IsSuperAdmin)
                return Results.Ok(clinic);

            if (tenantContext.ClinicId.HasValue && tenantContext.ClinicId == id)
                return Results.Ok(clinic);

            return Results.Forbid();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ViewAll)
        .WithAbac(
            Permissions.Clinics.ViewAll,
            "view",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
        .WithName("GetClinicById")
        .WithSummary("Get clinic by ID")
        .WithDescription("SuperAdmin: View any clinic. Clinic users: View own clinic only.")
        .Produces<ClinicResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // GET ALL CLINICS (SuperAdmin Only, Paginated)
        // ============================================
        group.MapGet("/", async (
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] IClinicService service) =>
        {
            var result = await service.GetAllClinicsPagedAsync(pagination);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ViewAll)
        .WithAbac<Clinic>(Permissions.Clinics.ViewAll, "list", r => r as Clinic)
        .WithName("GetAllClinicsPaged")
        .WithSummary("Get all clinics (SuperAdmin only, paginated)")
        .WithDescription("Returns a paginated list of all clinics.")
        .Produces<PaginatedResult<ClinicResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET ACTIVE CLINICS (SuperAdmin Only)
        // ============================================
        group.MapGet("/active", async (
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] IClinicService service) =>
        {
            var result = await service.GetActiveClinicsPagedAsync(pagination);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ViewAll)
        .WithAbac<Clinic>(Permissions.Clinics.ViewActive, "viewActive", r => r as Clinic)
        .WithName("GetActiveClinicsPaged")
        .WithSummary("Get all active clinics (SuperAdmin only, paginated)")
        .WithDescription("Returns paginated list of active clinics with valid subscriptions.")
        .Produces<PaginatedResult<ClinicResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // SEARCH CLINICS (SuperAdmin Only)
        // ============================================
        group.MapGet("/search", async (
            [FromQuery] string? query,
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] IClinicService service) =>
        {
            if (string.IsNullOrWhiteSpace(query))
                return Results.BadRequest(new { Error = "Search query is required" });

            var result = await service.SearchClinicsPagedAsync(query, pagination);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Search)
        .WithAbac<Clinic>(Permissions.Clinics.Search, "search", r => r as Clinic)
        .WithName("SearchClinicsPaged")
        .WithSummary("Search clinics (SuperAdmin only, paginated)")
        .WithDescription("Searches clinics by name, email, or phone with pagination.")
        .Produces<PaginatedResult<ClinicResponseDto>>(StatusCodes.Status200OK)
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

            if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != id)
                return Results.Forbid();

            var updated = await service.UpdateClinicAsync(id, dto);
            return updated != null
                ? Results.Ok(updated)
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Edit)
        .WithAbac(
            Permissions.Clinics.Edit,
            "edit",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
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
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ManageStatus)
        .WithAbac(
            Permissions.Clinics.ManageStatus,
            "changeStatus",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
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
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ManageStatus)
        .WithAbac(
            Permissions.Clinics.ManageStatus,
            "activate",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
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
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ManageStatus)
        .WithAbac(
            Permissions.Clinics.ManageStatus,
            "suspend",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
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
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Delete)
        .WithAbac(
            Permissions.Clinics.Delete,
            "delete",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
        .WithName("SoftDeleteClinic")
        .WithSummary("Soft delete a clinic (SuperAdmin only)")
        .WithDescription("Marks clinic as deleted. Can be restored. Users cannot login.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // HARD DELETE CLINIC (SuperAdmin Only)
        // ============================================
        group.MapDelete("/{id:guid}/permanent", async (
            Guid id,
            [FromServices] IClinicService service,
            [FromServices] ITenantContext tenantContext) =>
        {
            if (id == Guid.Empty)
                return Results.BadRequest(new { Error = "Invalid clinic ID" });

            if (!tenantContext.IsSuperAdmin)
                return Results.Forbid();

            var success = await service.DeleteClinicAsync(id);
            return success
                ? Results.Ok(new { Message = $"Clinic {id} permanently deleted" })
                : Results.NotFound(new { Error = $"Clinic {id} not found" });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.HardDelete)
        .WithAbac(
            Permissions.Clinics.HardDelete,
            "hardDelete",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
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
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ViewStats)
        .WithAbac(
            Permissions.Clinics.ViewStats,
            "viewStats",
            async ctx =>
            {
                var svc = ctx.RequestServices.GetRequiredService<IClinicService>();
                var idStr = ctx.Request.RouteValues["id"]?.ToString();
                return Guid.TryParse(idStr, out var cid)
                    ? await svc.GetClinicByIdAsync(cid)
                    : null;
            })
        .WithName("GetClinicStatistics")
        .WithSummary("Get clinic statistics (SuperAdmin only)")
        .WithDescription("Returns user count, subscription info, and other statistics.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}