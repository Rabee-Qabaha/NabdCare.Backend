using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Permissions; // Required for IPermissionEvaluator
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Api.Endpoints;

public static class ClinicEndpoints
{
    public static void MapClinicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clinics").WithTags("Clinics");

        // ============================================
        // 🆔 CREATE CLINIC (System Admin Only)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateClinicRequestDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<CreateClinicRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var created = await service.CreateClinicAsync(dto);
            return Results.Created($"/api/clinics/{created.Id}", created);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Create) // Strict: Only System Admins create clinics
        .WithAbac<Clinic>(Permissions.Clinics.Create, "create", r => r as Clinic)
        .WithName("CreateClinic")
        .Produces<ClinicResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🆔 UPDATE CLINIC (Hybrid: System Admin OR Owner)
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateClinicRequestDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<UpdateClinicRequestDto> validator,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IPermissionEvaluator perms) => // ✅ Inject Evaluator
        {
            if (id == Guid.Empty) return Results.BadRequest(new { Error = "Invalid clinic ID" });

            // 1. Check Permissions (OR Logic)
            // Allow if user is SuperAdmin/SystemAdmin (Clinics.Edit) OR Owner (Clinic.Edit)
            var isSystemAdmin = await perms.HasAsync(Permissions.Clinics.Edit);
            var isClinicOwner = await perms.HasAsync(Permissions.Clinic.Edit) && tenantContext.ClinicId == id;

            if (!isSystemAdmin && !isClinicOwner)
                return Results.Forbid();

            // 2. Validate DTO
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            // 3. Call Service (Service performs strict ownership checks for tenants)
            var updated = await service.UpdateClinicAsync(id, dto);
            
            return updated != null 
                ? Results.Ok(updated) 
                : Results.NotFound();
        })
        .RequireAuthorization()
        // ❌ REMOVED: .RequirePermission(Permissions.Clinics.Edit) 
        // We handle the permission check inside because it's conditional.
        .WithName("UpdateClinic")
        .Produces<ClinicResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
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
            return clinic != null ? Results.Ok(clinic) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinic.View)
        .WithName("GetMyClinic");

        // ============================================
        // GET BY ID (Hybrid: System Admin OR Owner)
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IClinicService service,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IPermissionEvaluator perms) =>
        {
            // 1. Check Permissions (OR Logic)
            // Allow if System View OR Own Clinic View
            var isSystemViewer = await perms.HasAsync(Permissions.Clinics.ViewAll);
            var isClinicViewer = await perms.HasAsync(Permissions.Clinic.View) && tenantContext.ClinicId == id;

            if (!isSystemViewer && !isClinicViewer)
                return Results.Forbid();

            var clinic = await service.GetClinicByIdAsync(id);
            if (clinic == null) return Results.NotFound();

            // Double check security (Service handles it, but endpoint filter is good practice)
            if (tenantContext.IsSuperAdmin || isSystemViewer || tenantContext.ClinicId == id)
                return Results.Ok(clinic);

            return Results.Forbid();
        })
        .RequireAuthorization()
        // ❌ REMOVED: .RequirePermission(Permissions.Clinics.ViewAll)
        .WithName("GetClinicById");

        // ============================================
        // GET ALL (Paginated) - System Admin Only
        // ============================================
        group.MapGet("/", async (
            [AsParameters] ClinicFilterRequestDto filters,
            [FromServices] IClinicService service) =>
        {
            var result = await service.GetAllClinicsPagedAsync(filters);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ViewAll) // ✅ Strict: Only Admins list all
        .WithName("GetAllClinicsPaged");

        // ============================================
        // SEARCH - System Admin Only
        // ============================================
        group.MapGet("/search", async (
            [FromQuery] string? query,
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] IClinicService service) =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Results.BadRequest(new { Error = "Query required" });
            var result = await service.SearchClinicsPagedAsync(query, pagination);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Search) // ✅ Strict: Only Admins search directory
        .WithName("SearchClinicsPaged");

        // ============================================
        // STATUS UPDATES (Admin Only)
        // ============================================
        group.MapPut("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateClinicStatusDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<UpdateClinicStatusDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) 
                throw new ValidationException(validation.Errors);

            var updated = await service.UpdateClinicStatusAsync(id, dto);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ManageStatus)
        .WithName("UpdateClinicStatus");

        group.MapPut("/{id:guid}/activate", async (Guid id, [FromServices] IClinicService service) =>
        {
            var updated = await service.ActivateClinicAsync(id);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ManageStatus)
        .WithName("ActivateClinic");

        group.MapPut("/{id:guid}/suspend", async (Guid id, [FromServices] IClinicService service) =>
        {
            var updated = await service.SuspendClinicAsync(id);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ManageStatus)
        .WithName("SuspendClinic");

        // ============================================
        // DELETE & RESTORE (Admin Only)
        // ============================================
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IClinicService service) =>
        {
            var success = await service.SoftDeleteClinicAsync(id);
            return success ? Results.Ok(new { Message = "Clinic soft deleted" }) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Delete)
        .WithName("SoftDeleteClinic");

        group.MapPut("/{id:guid}/restore", async (Guid id, [FromServices] IClinicService service) =>
        {
            var success = await service.RestoreClinicAsync(id);
            return success ? Results.Ok(new { Message = "Clinic restored" }) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.Restore)
        .WithName("RestoreClinic");

        group.MapDelete("/{id:guid}/permanent", async (Guid id, [FromServices] IClinicService service, [FromServices] ITenantContext ctx) =>
        {
            if (!ctx.IsSuperAdmin) return Results.Forbid();
            var success = await service.DeleteClinicAsync(id);
            return success ? Results.Ok(new { Message = "Clinic permanently deleted" }) : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.HardDelete)
        .WithName("HardDeleteClinic");

        // ============================================
        // STATISTICS
        // ============================================
        // group.MapGet("/{id:guid}/stats", async (Guid id, [FromServices] IClinicService service) =>
        // {
        //     var stats = await service.GetClinicStatisticsAsync(id);
        //     return stats != null ? Results.Ok(stats) : Results.NotFound();
        // })
        // .RequireAuthorization()
        // .RequirePermission(Permissions.Clinics.ViewStats)
        // .WithName("GetClinicStatistics");
    }
}