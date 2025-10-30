using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Audit;
using NabdCare.Api.Extensions;

namespace NabdCare.Api.Endpoints;

public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/audit-logs")
            .WithTags("Audit Logs");

        // ============================================
        // 📋 GET AUDIT LOGS (CURSOR-BASED PAGINATION)
        // ============================================
        group.MapGet("/", async (
                [AsParameters] AuditLogListRequestDto filter,
                [AsParameters] PaginationRequestDto pagination,
                [FromServices] IAuditLogRepository repo) =>
            {
                var result = await repo.GetPagedAsync(filter, pagination);

                return Results.Ok(new
                {
                    result.TotalCount,
                    result.HasMore,
                    result.NextCursor,
                    result.Items
                });
            })
            .RequirePermission("AuditLogs.View")
            .WithSummary("List audit logs with filtering, sorting, and cursor-based pagination")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Description = """
                              Retrieves audit logs with optional filters (Action, EntityType, UserId, date range, search text)
                              and supports cursor-based pagination (Limit, Cursor) with sorting options.
                              """
            });
    }
}