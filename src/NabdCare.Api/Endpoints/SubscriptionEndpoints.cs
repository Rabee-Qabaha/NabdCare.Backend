using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Constants;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscriptions").WithTags("Subscriptions");

        // ============================================
        // 🆕 GET AVAILABLE PLANS (Public/Authenticated)
        // ============================================
        group.MapGet("/plans", () => Results.Ok(SubscriptionPlans.All))
            .RequireAuthorization()
            .WithName("GetSubscriptionPlans")
            .Produces<IEnumerable<PlanDefinition>>(StatusCodes.Status200OK);

        // ============================================
        // 🔹 GET SUBSCRIPTION BY ID (Hybrid)
        // ============================================
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenant,
            [FromServices] IPermissionEvaluator perms) =>
        {
            // 1. Fetch
            var sub = await service.GetByIdAsync(id, includePayments: false);
            if (sub == null) return Results.NotFound();

            // 2. 🔐 Security: System Admin OR Owner
            var isSystem = tenant.IsSuperAdmin;
            var isOwner = tenant.ClinicId == sub.ClinicId;

            if (!isSystem && !isOwner) return Results.Forbid();
            
            // If Owner, must have View permission
            if (isOwner && !await perms.HasAsync(Permissions.Subscriptions.View))
                return Results.Forbid();

            return Results.Ok(sub);
        })
        .RequireAuthorization()
        .WithName("GetSubscriptionById")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // 🔹 GET ACTIVE SUBSCRIPTION FOR CLINIC
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}/active", async (
                Guid clinicId,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenant,
                [FromServices] IPermissionEvaluator perms) =>
            {
                // 🔐 Security: System Admin OR Specific Clinic Owner
                if (!tenant.IsSuperAdmin && tenant.ClinicId != clinicId)
                    return Results.Forbid();

                if (!tenant.IsSuperAdmin && !await perms.HasAsync(Permissions.Subscriptions.ViewActive))
                     return Results.Forbid();

                var active = await service.GetActiveSubscriptionAsync(clinicId);

                // Frontend preference: 200 OK with null if no active plan
                if (active == null) return Results.Ok<object?>(null);

                var daysRemaining = (active.EndDate - DateTime.UtcNow).Days;

                return Results.Ok(new
                {
                    Subscription = active,
                    DaysRemaining = daysRemaining,
                    IsExpiringSoon = daysRemaining <= 30 && daysRemaining > 0,
                    IsExpired = daysRemaining < 0
                });
            })
            .RequireAuthorization()
            .WithName("GetActiveSubscriptionForClinic");

        // ============================================
        // 🔹 GET SUBSCRIPTIONS BY CLINIC (HISTORY)
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
                Guid clinicId,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenant,
                [FromServices] IPermissionEvaluator perms,
                [AsParameters] PaginationRequestDto pagination,
                [FromQuery] bool includePayments = false) =>
            {
                // 🔐 Security
                if (!tenant.IsSuperAdmin && tenant.ClinicId != clinicId)
                    return Results.Forbid();

                if (!tenant.IsSuperAdmin && !await perms.HasAsync(Permissions.Subscriptions.View))
                    return Results.Forbid();

                var result = await service.GetByClinicIdPagedAsync(clinicId, pagination, includePayments);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithName("GetSubscriptionsByClinic");

        // ============================================
        // 🔹 GET ALL SUBSCRIPTIONS (SuperAdmin Only)
        // ============================================
        group.MapGet("/", async (
            [AsParameters] PaginationRequestDto pagination,
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenant, 
            [FromQuery] bool includePayments = false) =>
        {
            // 🔐 Strict Security
            if (!tenant.IsSuperAdmin) return Results.Forbid();

            var result = await service.GetAllPagedAsync(pagination, includePayments);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.ViewAll)
        .WithName("GetAllSubscriptionsPaged");

        // ============================================
        // 🔹 CREATE SUBSCRIPTION (Admin Only)
        // ============================================
        group.MapPost("/", async (
            [FromBody] CreateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<CreateSubscriptionRequestDto> validator) =>
        {
            // 1. Validate
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);

            // 2. Create (Service handles Domain Exceptions like Clinic Not Found)
            var created = await service.CreateSubscriptionAsync(dto);
            return Results.Created($"/api/subscriptions/{created.Id}", created);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.Create) 
        .WithName("CreateSubscription")
        .Produces<SubscriptionResponseDto>(StatusCodes.Status201Created);

        // ============================================
        // 🔹 UPDATE SUBSCRIPTION (Hybrid)
        // ============================================
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSubscriptionRequestDto dto,
            [FromServices] ISubscriptionService service,
            [FromServices] IValidator<UpdateSubscriptionRequestDto> validator,
            [FromServices] ITenantContext tenant,
            [FromServices] IPermissionEvaluator perms) =>
        {
            // 1. Check Existence
            var sub = await service.GetByIdAsync(id, false);
            if (sub == null) return Results.NotFound();

            // 2. 🔐 Security: System Admin OR Owner
            var isSystem = tenant.IsSuperAdmin;
            var isOwner = tenant.ClinicId == sub.ClinicId;

            if (!isSystem && !isOwner) return Results.Forbid();
            if (isOwner && !await perms.HasAsync(Permissions.Subscriptions.Edit))
                return Results.Forbid();

            // 3. Validation
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);

            // 4. Update
            var updated = await service.UpdateSubscriptionAsync(id, dto);
            return Results.Ok(updated);
        })
        .RequireAuthorization()
        .WithName("UpdateSubscription");

        // ============================================
        // 🔹 CHANGE STATUS (Admin Only)
        // ============================================
        group.MapPatch("/{id:guid}/status", async (
                Guid id,
                [FromBody] SubscriptionStatus newStatus,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenant) =>
            {
                if (!tenant.IsSuperAdmin) return Results.Forbid();

                var updated = await service.UpdateSubscriptionStatusAsync(id, newStatus);
                return updated != null
                    ? Results.Ok(new { Message = $"Subscription {id} status changed to {newStatus}" })
                    : Results.NotFound();
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Subscriptions.ChangeStatus)
            .WithName("ChangeSubscriptionStatus");

        // ============================================
        // 🔹 RENEW (Admin / Owner)
        // ============================================
        group.MapPost("/{id:guid}/renew", async (
                Guid id,
                [FromQuery] SubscriptionType type,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenant,
                [FromServices] IPermissionEvaluator perms) =>
            {
                // 1. Check
                var sub = await service.GetByIdAsync(id, false);
                if (sub == null) return Results.NotFound();

                // 2. 🔐 Security
                var isSystem = tenant.IsSuperAdmin;
                var isOwner = tenant.ClinicId == sub.ClinicId;

                if (!isSystem && !isOwner) return Results.Forbid();
                if (isOwner && !await perms.HasAsync(Permissions.Subscriptions.Renew))
                    return Results.Forbid();

                // 3. Execute
                var renewed = await service.RenewSubscriptionAsync(id, type);
                return Results.Ok(new { Message = "Renewed successfully", Subscription = renewed });
            })
            .RequireAuthorization()
            .WithName("RenewSubscription");

        // ============================================
        // 🔹 TOGGLE AUTO-RENEW (Admin / Owner)
        // ============================================
        group.MapPatch("/{id:guid}/auto-renew", async (
                Guid id,
                [FromQuery] bool enable,
                [FromServices] ISubscriptionService service,
                [FromServices] ITenantContext tenant,
                [FromServices] IPermissionEvaluator perms) =>
            {
                var sub = await service.GetByIdAsync(id, false);
                if (sub == null) return Results.NotFound();

                var isSystem = tenant.IsSuperAdmin;
                var isOwner = tenant.ClinicId == sub.ClinicId;

                if (!isSystem && !isOwner) return Results.Forbid();
                if (isOwner && !await perms.HasAsync(Permissions.Subscriptions.ToggleAutoRenew))
                    return Results.Forbid();

                var updated = await service.ToggleAutoRenewAsync(id, enable);
                return Results.Ok(updated);
            })
            .RequireAuthorization()
            .WithName("ToggleAutoRenew");

        // ============================================
        // 🔹 CANCEL (Hybrid)
        // ============================================
        group.MapDelete("/{id:guid}", async (
            Guid id, 
            [FromServices] ISubscriptionService service,
            [FromServices] ITenantContext tenant,
            [FromServices] IPermissionEvaluator perms) =>
        {
            var sub = await service.GetByIdAsync(id, false);
            if (sub == null) return Results.NotFound();

            var isSystem = tenant.IsSuperAdmin;
            var isOwner = tenant.ClinicId == sub.ClinicId;

            if (!isSystem && !isOwner) return Results.Forbid();
            if (isOwner && !await perms.HasAsync(Permissions.Subscriptions.Cancel))
                return Results.Forbid();

            var success = await service.CancelSubscriptionAsync(id);
            return success 
                ? Results.Ok(new { Message = "Subscription cancelled successfully." }) 
                : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("CancelSubscription");

        // ============================================
        // 🔹 HARD DELETE (Admin Only)
        // ============================================
        group.MapDelete("/{id:guid}/hard", async (
            Guid id, 
            [FromServices] ISubscriptionService service, 
            [FromServices] ITenantContext tenant) =>
        {
            if (!tenant.IsSuperAdmin) return Results.Forbid();

            var success = await service.DeleteSubscriptionAsync(id);
            return success 
                ? Results.Ok(new { Message = "Subscription and all history permanently deleted." }) 
                : Results.NotFound();
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Subscriptions.HardDelete)
        .WithName("HardDeleteSubscription");
    }
}