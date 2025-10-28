using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Api.Extensions;
using NabdCare.Application.Interfaces.Audit;

namespace NabdCare.Api.Endpoints;

public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/audit-logs")
            .WithTags("Audit Logs");

        group.MapGet("/", async (
                [AsParameters] AuditLogListRequestDto req,
                [FromServices] IAuditLogRepository repo) =>
            {
                var (items, total) = await repo.GetAuditLogsAsync(req);

                return Results.Ok(new
                {
                    total,
                    page = req.Page,
                    pageSize = req.PageSize,
                    items
                });
            })
            .RequirePermission("AuditLogs.View")
            .WithSummary("Search & list audit logs with filters and paging");
    }
}