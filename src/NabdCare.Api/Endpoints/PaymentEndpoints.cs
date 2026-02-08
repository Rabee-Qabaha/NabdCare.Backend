using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Payments;
using NabdCare.Application.Interfaces.Payments;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("payments")
            .WithTags("Payments");

        // ============================================
        // CREATE PAYMENT
        // ============================================
        group.MapPost("/", async (
                [FromBody] CreatePaymentRequestDto request,
                [FromServices] IPaymentService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                var result = await service.CreatePaymentAsync(request);
                return Results.Created($"/payments/{result.Id}", result);
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.Create)
            .WithName("CreatePayment")
            .WithSummary("Record a new payment")
            .Produces<PaymentDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET PAYMENT BY ID
        // ============================================
        group.MapGet("/{id:guid}", async (Guid id, [FromServices] IPaymentService service) =>
            {
                var payment = await service.GetPaymentByIdAsync(id);
                return payment != null ? Results.Ok(payment) : Results.NotFound();
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.View)
            .WithName("GetPaymentById")
            .Produces<PaymentDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET PAYMENTS BY CLINIC (B2B / B2C) - PAGED & FILTERED
        // ============================================
        group.MapGet("/clinic/{clinicId:guid}", async (
                Guid clinicId, 
                [AsParameters] PaymentFilterRequestDto filter,
                [FromServices] IPaymentService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.IsSuperAdmin && tenantContext.ClinicId != clinicId)
                {
                    return Results.Forbid();
                }

                var payments = await service.GetPaymentsByClinicPagedAsync(clinicId, filter);
                return Results.Ok(payments);
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.View)
            .WithName("GetClinicPayments")
            .WithSummary("Get all payments for a specific clinic (Paged & Filtered)")
            .Produces<PaginatedResult<PaymentDto>>()
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // ALLOCATE PAYMENT (Apply Credit)
        // ============================================
        group.MapPost("/{paymentId:guid}/allocate", async (
                Guid paymentId,
                [FromBody] PaymentAllocationRequestDto request,
                [FromServices] IPaymentService service) =>
            {
                await service.AllocatePaymentToInvoiceAsync(paymentId, request.InvoiceId, request.Amount);
                return Results.Ok(new { message = "Payment allocated successfully." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.Allocate)
            .WithName("AllocatePayment")
            .WithSummary("Allocate unallocated funds to an invoice")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // DEALLOCATE PAYMENT (Unlink)
        // ============================================
        group.MapPost("/{paymentId:guid}/deallocate/{invoiceId:guid}", async (
                Guid paymentId,
                Guid invoiceId,
                [FromServices] IPaymentService service) =>
            {
                await service.DeallocatePaymentAsync(paymentId, invoiceId);
                return Results.Ok(new { message = "Payment deallocated successfully." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.Allocate)
            .WithName("DeallocatePayment")
            .WithSummary("Unlink a payment from an invoice (return to credit)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // CANCEL PAYMENT (Void)
        // ============================================
        group.MapPost("/{paymentId:guid}/cancel", async (
                Guid paymentId,
                [FromBody] CancelPaymentRequestDto request,
                [FromServices] IPaymentService service) =>
            {
                await service.CancelPaymentAsync(paymentId, request.Reason);
                return Results.Ok(new { message = "Payment cancelled successfully." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.Cancel)
            .WithName("CancelPayment")
            .WithSummary("Void a payment (reverse all allocations)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // REFUND PAYMENT
        // ============================================
        group.MapPost("/{paymentId:guid}/refund", async (
                Guid paymentId,
                [FromBody] RefundRequestDto request,
                [FromServices] IPaymentService service) =>
            {
                await service.RefundPaymentAsync(paymentId, request.Reason, request.Amount);
                return Results.Ok(new { message = "Payment refunded successfully." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.Refund)
            .WithName("RefundPayment")
            .WithSummary("Refund a payment (partial or full)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // UPDATE CHEQUE STATUS
        // ============================================
        group.MapPatch("/{paymentId:guid}/cheque-status", async (
                Guid paymentId,
                [FromBody] UpdateChequeStatusDto request,
                [FromServices] IPaymentService service) =>
            {
                await service.UpdateChequeStatusAsync(paymentId, request.Status);
                return Results.Ok(new { message = "Cheque status updated successfully." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.ManageCheques)
            .WithName("UpdateChequeStatus")
            .WithSummary("Update cheque status (Clear, Bounce, Cancel)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // UPDATE CHEQUE DETAILS
        // ============================================
        group.MapPut("/{paymentId:guid}/cheque", async (
                Guid paymentId,
                [FromBody] UpdateChequeDetailDto request,
                [FromServices] IPaymentService service) =>
            {
                await service.UpdateChequeDetailsAsync(paymentId, request);
                return Results.Ok(new { message = "Cheque details updated successfully." });
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.ManageCheques)
            .WithName("UpdateChequeDetails")
            .WithSummary("Edit cheque details (only if Pending)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }
}