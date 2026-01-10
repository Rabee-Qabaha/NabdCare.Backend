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
            [FromServices] IBranchService service,
            [FromServices] ITenantContext context) =>
        {
            var branch = await service.GetBranchByIdAsync(id);
            
            if (branch == null) return Results.NotFound();

            if (!context.IsSuperAdmin && context.ClinicId != branch.ClinicId)
                return Results.Forbid();

            return Results.Ok(branch);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.View)
        .WithAbac(Permissions.Branches.View, "view", async ctx => 
        {
             var svc = ctx.RequestServices.GetRequiredService<IBranchService>();
             var idStr = ctx.Request.RouteValues["id"]?.ToString();
             return Guid.TryParse(idStr, out var bid) ? await svc.GetBranchByIdAsync(bid) : null; 
        })
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

            if (!context.IsSuperAdmin && dto.ClinicId != context.ClinicId)
                return Results.Forbid();

            try 
            {
                var created = await service.CreateBranchAsync(dto);
                return Results.Created($"/api/branches/{created.Id}", created);
            }
            catch (InvalidOperationException ex) 
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
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
            [FromServices] IValidator<UpdateBranchRequestDto> validator,
            [FromServices] ITenantContext context,
            [FromServices] IPermissionEvaluator perms) => // Inject Perms Evaluator
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) 
                return Results.BadRequest(new { Errors = validation.Errors.Select(e => e.ErrorMessage) });

            // 1. Check Permissions for Setting Main Branch
            if (dto.IsMain)
            {
                if (!await perms.HasAsync(Permissions.Branches.SetMain))
                    return Results.Forbid();
            }

            // 2. Existence Check
            var existing = await service.GetBranchByIdAsync(id);
            if (existing == null) return Results.NotFound();

            if (!context.IsSuperAdmin && existing.ClinicId != context.ClinicId)
                return Results.Forbid();

            try {
                var updated = await service.UpdateBranchAsync(id, dto);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex) {
                 return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.Edit)
        .WithAbac(Permissions.Branches.Edit, "edit", async ctx => 
        {
             var svc = ctx.RequestServices.GetRequiredService<IBranchService>();
             var idStr = ctx.Request.RouteValues["id"]?.ToString();
             return Guid.TryParse(idStr, out var bid) ? await svc.GetBranchByIdAsync(bid) : null; 
        })
        .WithName("UpdateBranch")
        .Produces<BranchResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // ============================================
        // 🔹 TOGGLE STATUS (Patch)
        // ============================================
        group.MapPatch("/{id:guid}/toggle-status", async (
            Guid id,
            [FromServices] IBranchService service,
            [FromServices] ITenantContext context) =>
        {
            var existing = await service.GetBranchByIdAsync(id);
            if (existing == null) return Results.NotFound();

            if (!context.IsSuperAdmin && existing.ClinicId != context.ClinicId)
                return Results.Forbid();

            try {
                var updated = await service.ToggleBranchStatusAsync(id);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex) {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.ToggleStatus) // ✅ UPDATED: Specific Permission
        .WithAbac(Permissions.Branches.ToggleStatus, "edit", async ctx => 
        {
             var svc = ctx.RequestServices.GetRequiredService<IBranchService>();
             var idStr = ctx.Request.RouteValues["id"]?.ToString();
             return Guid.TryParse(idStr, out var bid) ? await svc.GetBranchByIdAsync(bid) : null; 
        })
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
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Branches.Delete)
        .WithAbac(Permissions.Branches.Delete, "delete", async ctx => 
        {
             var svc = ctx.RequestServices.GetRequiredService<IBranchService>();
             var idStr = ctx.Request.RouteValues["id"]?.ToString();
             return Guid.TryParse(idStr, out var bid) ? await svc.GetBranchByIdAsync(bid) : null; 
        })
        .WithName("DeleteBranch")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
    }
}