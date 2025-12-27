using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Application.Validator.Clinics.Branches;

namespace NabdCare.Api.Endpoints;

public static class BranchEndpoints
{
    public static void MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/branches").WithTags("Branches");

        // ============================================
        // 🔹 GET BRANCHES (Unified: Global or Clinic-Specific)
        // ============================================
        group.MapGet("/", async (
            [FromQuery] Guid? clinicId,
            [FromServices] IBranchService service,
            [FromServices] ITenantContext context) =>
        {
            Guid? targetClinicId;

            // SCENARIO 1: SuperAdmin
            if (context.IsSuperAdmin)
            {
                // Can request specific clinic OR all (null)
                targetClinicId = clinicId; 
            }
            // SCENARIO 2: Clinic User
            else
            {
                // FORCE their own clinic ID (Security)
                if (!context.ClinicId.HasValue) 
                    return Results.BadRequest(new { Error = "Clinic context required." });
                
                targetClinicId = context.ClinicId.Value;
            }
            
            var branches = await service.GetBranchesAsync(targetClinicId);
            return Results.Ok(branches);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.View)
        .WithName("GetBranches")
        .WithSummary("Get branches")
        .WithDescription("SuperAdmin: Can filter by clinicId or get all. Clinic Users: Always gets own branches.")
        .Produces<List<BranchResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 GET BRANCH BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IBranchService service,
            [FromServices] ITenantContext context) =>
        {
            var branch = await service.GetBranchByIdAsync(id);
            
            if (branch == null) 
                return Results.NotFound();

            // Security: Ensure user owns this branch (if not SuperAdmin)
            if (!context.IsSuperAdmin && context.ClinicId != branch.ClinicId)
                return Results.Forbid();

            return Results.Ok(branch);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.View)
        .WithName("GetBranchById")
        .Produces<BranchResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 CREATE BRANCH
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateBranchRequestDto dto,
            [FromServices] IBranchService service,
            [FromServices] IValidator<CreateBranchRequestDto> validator,
            [FromServices] ITenantContext context) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) 
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            // Security: Ensure Clinic Admin is creating for their own clinic
            if (!context.IsSuperAdmin && dto.ClinicId != context.ClinicId)
                return Results.Forbid();

            try 
            {
                var created = await service.CreateBranchAsync(dto);
                return Results.Created($"/api/branches/{created.Id}", created);
            }
            catch (InvalidOperationException ex) // Catch Limit Exceeded
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.Create)
        .WithName("CreateBranch")
        .Produces<BranchResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 UPDATE BRANCH
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateBranchRequestDto dto,
            [FromServices] IBranchService service,
            [FromServices] IValidator<UpdateBranchRequestDto> validator,
            [FromServices] ITenantContext context) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) 
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            // 1. Check existence & ownership before update
            var existing = await service.GetBranchByIdAsync(id);
            if (existing == null) return Results.NotFound();

            if (!context.IsSuperAdmin && existing.ClinicId != context.ClinicId)
                return Results.Forbid();

            var updated = await service.UpdateBranchAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.Edit)
        .WithName("UpdateBranch")
        .Produces<BranchResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 DELETE BRANCH
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IBranchService service,
            [FromServices] ITenantContext context) =>
        {
            var existing = await service.GetBranchByIdAsync(id);
            if (existing == null) return Results.NotFound();

            if (!context.IsSuperAdmin && existing.ClinicId != context.ClinicId)
                return Results.Forbid();

            try
            {
                await service.DeleteBranchAsync(id);
                return Results.NoContent(); // 204 Success
            }
            catch (InvalidOperationException ex) // Catch Main Branch deletion attempt
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.Delete)
        .WithName("DeleteBranch")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
    }
}