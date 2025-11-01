using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Audit;
using NabdCare.Api.Extensions;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Domain.Entities.Audits;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Secure endpoints for retrieving system audit logs with RBAC + ABAC.
/// Author: Rabee Qabaha
/// Updated: 2025-10-31 ✅
/// </summary>
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
                [FromServices] IAuditLogRepository repo,
                [FromServices] ITenantContext tenant) =>
        {
            // ✅ ABAC Filter: Clinic admins only see their own clinic logs
            Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? abacFilter = null;

            if (!tenant.IsSuperAdmin && tenant.ClinicId.HasValue)
            {
                var clinicId = tenant.ClinicId.Value;
                abacFilter = q => q.Where(a => a.ClinicId == clinicId);
            }

            var result = await repo.GetPagedAsync(filter, pagination, abacFilter);

            return Results.Ok(new
            {
                result.TotalCount,
                result.HasMore,
                result.NextCursor,
                result.Items
            });
        })
        .RequireAuthorization()
        .RequirePermission(Permissions.AuditLogs.View)
        .WithAbac<AuditLog>(
            Permissions.AuditLogs.View,
            "list",
            async ctx =>
            {
                var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();

                // SuperAdmin → unrestricted access (return null)
                if (tenant.IsSuperAdmin)
                    return null;

                // ClinicAdmin → return dummy AuditLog with ClinicId for ABAC evaluation
                return await Task.FromResult(new AuditLog
                {
                    ClinicId = tenant.ClinicId ?? Guid.Empty,
                    Action = "ContextFilter",
                    EntityType = "AuditLog"
                });
            })
        .WithSummary("List audit logs (SuperAdmin = all, ClinicAdmin = own clinic)")
        .WithDescription("""
                         Retrieves audit logs with optional filters (Action, EntityType, UserId, date range, search text)
                         and supports cursor-based pagination.
                         SuperAdmins can view all logs, while ClinicAdmins only see logs belonging to their clinic.
                         """)
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new OpenApiOperation(operation)
        {
            Description = """
                          Retrieves audit logs with optional filters (Action, EntityType, UserId, date range, search text)
                          and supports cursor-based pagination (Limit, Cursor) with sorting options.
                          SuperAdmins can view all logs, while ClinicAdmins only see logs belonging to their clinic.
                          """
        });
    }
}