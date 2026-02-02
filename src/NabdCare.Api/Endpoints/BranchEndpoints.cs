using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Application.Interfaces.Permissions; // Added for IPermissionEvaluator
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Api.Endpoints;

public static class BranchEndpoints
{
    public static void MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/branches").WithTags("Branches");

        // ============================================
        // 🔹 GET BRANCHES
        // ============================================
        group.MapGet("/", async (
            [FromQuery] Guid? clinicId,
            [FromServices] IBranchService service,
            [FromServices] ITenantContext context) =>
        {
            Guid? targetClinicId;

            if (context.IsSuperAdmin)
            {
                targetClinicId = clinicId; 
            }
            else
            {
                if (!context.ClinicId.HasValue) 
                    return Results.BadRequest(new { Error = "Clinic context required." });
                
                targetClinicId = context.ClinicId.Value;
            }
            
            var branches = await service.GetBranchesAsync(targetClinicId);
            return Results.Ok(branches);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.View)
        .WithAbac<Branch>(Permissions.Branches.View, "list", r => r as Branch)
        .WithName("GetBranches")
        .Produces<List<BranchResponseDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 GET BRANCH BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IBranchService service) =>
        {
            var branch = await service.GetBranchByIdAsync(id);
            if (branch == null) return Results.NotFound();

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
            [FromServices] IValidator<CreateBranchRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) 
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            var created = await service.CreateBranchAsync(dto);
            return Results.Created($"/api/branches/{created.Id}", created);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.Create)
        .WithAbac<Branch>(Permissions.Branches.Create, "create", r => r as Branch)
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
            [FromServices] IValidator<UpdateBranchRequestDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) 
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

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
        // 🔹 TOGGLE STATUS (Patch)
        // ============================================
        group.MapPatch("/{id:guid}/toggle-status", async (
            Guid id,
            [FromServices] IBranchService service) =>
        {
            var updated = await service.ToggleBranchStatusAsync(id);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.ToggleStatus) 
        .WithName("ToggleBranchStatus")
        .WithSummary("Activate/Deactivate a branch")
        .Produces<BranchResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 DELETE BRANCH
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IBranchService service) =>
        {
            await service.DeleteBranchAsync(id);
            return Results.NoContent();
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