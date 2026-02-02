using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces.Reports;

namespace NabdCare.Api.Endpoints;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("reports")
            .WithTags("Reports");

        // ============================================
        // GET CLINIC STATEMENT (SOA)
        // ============================================
        group.MapGet("/statement", async (
                [FromQuery] DateTime start,
                [FromQuery] DateTime end,
                [FromServices] IReportService service,
                [FromServices] ITenantContext tenantContext) =>
            {
                if (!tenantContext.ClinicId.HasValue)
                {
                    return Results.BadRequest("Clinic context required.");
                }

                var statement = await service.GetClinicStatementAsync(tenantContext.ClinicId.Value, start, end);
                return Results.Ok(statement);
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Reports.ViewFinancialReports)
            .WithName("GetClinicStatement")
            .WithSummary("Get Statement of Account for a date range")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);

        // ============================================
        // GET PAYMENT RECEIPT
        // ============================================
        group.MapGet("/receipt/{paymentId:guid}", async (
                Guid paymentId,
                [FromServices] IReportService service) =>
            {
                var receipt = await service.GetPaymentReceiptAsync(paymentId);
                return Results.Ok(receipt);
            })
            .RequireAuthorization()
            .RequirePermission(Permissions.Payments.ViewReceipts)
            .WithName("GetPaymentReceipt")
            .WithSummary("Get receipt details for a payment")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}