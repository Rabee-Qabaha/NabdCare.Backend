using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Configuration;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

public static class ClinicEndpoints
{
    public static void MapClinicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clinics").WithTags("Clinics");

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
        .RequirePermission(Permissions.Clinics.Create)
        .WithAbac<Clinic>(Permissions.Clinics.Create, "create", r => r as Clinic)
        .WithName("CreateClinic")
        .Produces<ClinicResponseDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateClinicRequestDto dto,
            [FromServices] IClinicService service,
            [FromServices] IValidator<UpdateClinicRequestDto> validator) =>
        {
            if (id == Guid.Empty) return Results.BadRequest(new { Error = "Invalid clinic ID" });

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var updated = await service.UpdateClinicAsync(id, dto);
            
            return updated != null 
                ? Results.Ok(updated) 
                : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("UpdateClinic")
        .Produces<ClinicResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

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

        group.MapGet("/me/exchange-rate", async (
            [FromQuery] Currency targetCurrency,
            [FromServices] ITenantContext tenantContext,
            [FromServices] IClinicRepository clinicRepository,
            [FromServices] IExchangeRateService exchangeRateService) =>
        {
            if (tenantContext.IsSuperAdmin || !tenantContext.ClinicId.HasValue)
            {
                return Results.BadRequest(new { Error = "This endpoint is for clinic users only." });
            }

            var clinic = await clinicRepository.GetByIdAsync(tenantContext.ClinicId.Value);
            if (clinic == null)
            {
                return Results.NotFound(new { Error = "Clinic not found." });
            }

            var functionalCurrency = clinic.Settings.Currency;

            if (targetCurrency == functionalCurrency)
            {
                return Results.Ok(new ExchangeRateResponseDto
                {
                    BaseRate = 1.0m,
                    FinalRate = 1.0m,
                    MarkupType = MarkupType.None,
                    MarkupValue = 0,
                    FunctionalCurrency = functionalCurrency.ToString(),
                    TargetCurrency = targetCurrency.ToString()
                });
            }

            var baseRate = await exchangeRateService.GetRateAsync(functionalCurrency.ToString(), targetCurrency.ToString());
            
            var finalRate = baseRate;
            if (clinic.Settings.ExchangeRateMarkupType == MarkupType.Percentage && clinic.Settings.ExchangeRateMarkupValue > 0)
            {
                var markup = baseRate * (clinic.Settings.ExchangeRateMarkupValue / 100);
                finalRate += markup;
            }

            return Results.Ok(new ExchangeRateResponseDto
            {
                BaseRate = baseRate,
                FinalRate = finalRate,
                MarkupType = clinic.Settings.ExchangeRateMarkupType,
                MarkupValue = clinic.Settings.ExchangeRateMarkupValue,
                FunctionalCurrency = functionalCurrency.ToString(),
                TargetCurrency = targetCurrency.ToString()
            });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Payments.View)
        .WithName("GetClinicExchangeRate")
        .Produces<ExchangeRateResponseDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IClinicService service) =>
        {
            var clinic = await service.GetClinicByIdAsync(id);
            if (clinic == null) return Results.NotFound();

            return Results.Ok(clinic);
        })
        .RequireAuthorization()
        .WithName("GetClinicById");

        group.MapGet("/", async (
            [AsParameters] ClinicFilterRequestDto filters,
            [FromServices] IClinicService service) =>
        {
            var result = await service.GetAllClinicsPagedAsync(filters);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.Clinics.ViewAll)
        .WithName("GetAllClinicsPaged");

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
        .RequirePermission(Permissions.Clinics.Search)
        .WithName("SearchClinicsPaged");

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
    }
}