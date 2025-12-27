using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Extensions; // For RequirePermission extension
using NabdCare.Application.Common.Constants;
using NabdCare.Infrastructure.BackgroundJobs;

namespace NabdCare.Api.Endpoints;

public static class JobsEndpoints
{
    public static void MapJobsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/jobs")
            .WithTags("Admin Jobs")
            .RequireAuthorization();

        // ============================================
        // 🔄 FORCE RUN: SUBSCRIPTION LIFECYCLE
        // ============================================
        group.MapPost("/subscription-lifecycle", async (
                [FromServices] SubscriptionLifecycleJob job) =>
            {
                // Executes the 3-Phase Logic:
                // 1. Activates Future Plans
                // 2. Processes Auto-Renewals
                // 3. Expires Ended Plans
                await job.ExecuteAsync();
                
                return Results.Ok(new 
                { 
                    Message = "Subscription lifecycle job triggered successfully.", 
                    Note = "Check application logs for operation counts." 
                });
            })
            // 🔒 CRITICAL: Only SuperAdmin can touch this.
            // We use 'ChangeStatus' because this job's primary function is changing statuses.
            .RequirePermission(Permissions.Subscriptions.ChangeStatus) 
            .WithName("RunSubscriptionLifecycleJob")
            .WithSummary("Manually trigger the daily subscription lifecycle job")
            .WithDescription("Forces the daily background process: Activates queued future plans, processes auto-renewals, and expires ended subscriptions.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}