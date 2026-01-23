using NabdCare.Api.Extensions;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces.Clinics;

namespace NabdCare.Api.Endpoints;

public static class ClinicDashboardEndpoints
{
    public static void MapClinicDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("clinics")
            .WithTags("Clinics Dashboard")
            .RequireAuthorization();

        group.MapGet("/{id:guid}/stats", async (Guid id, IClinicDashboardService service) =>
            {
                var stats = await service.GetStatsAsync(id);
                return Results.Ok(stats);
            })
            .RequirePermission(Permissions.Clinics.View)
            .WithName("GetClinicDashboardStats")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get aggregated statistics for a specific clinic",
                Description = "Returns fast counts for users, branches, patients, and subscription health."
            });
    }
}