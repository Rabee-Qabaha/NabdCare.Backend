using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.Authorizations;
using NabdCare.Application.Interfaces.Authorizations;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Authorization endpoints for checking if user can perform actions on resources.
/// Supports resource-level ABAC authorization checks.
/// 
/// Author: Rabee Qabaha
/// Updated: 2025-11-02
/// </summary>
public static class AuthorizationEndpoints
{
    public static void MapAuthorizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/authorization").WithTags("Authorization");

        // ============================================
        // CHECK AUTHORIZATION
        // ============================================
        group.MapPost("/check", async (
            [FromBody] AuthorizationCheckRequestDto request,
            [FromServices] IAuthorizationService authService) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        message = "Request body is required",
                        type = "ValidationError",
                        statusCode = 400
                    }
                });
            }

            if (string.IsNullOrWhiteSpace(request.ResourceType))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        message = "ResourceType is required",
                        type = "ValidationError",
                        statusCode = 400
                    }
                });
            }

            if (string.IsNullOrWhiteSpace(request.ResourceId))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        message = "ResourceId is required",
                        type = "ValidationError",
                        statusCode = 400
                    }
                });
            }

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        message = "Action is required",
                        type = "ValidationError",
                        statusCode = 400
                    }
                });
            }

            try
            {
                var result = await authService.CheckAuthorizationAsync(
                    request.ResourceType,
                    request.ResourceId,
                    request.Action);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .RequireAuthorization()
        .Produces<AuthorizationResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithName("CheckAuthorization")
        .WithSummary("Check if current user can perform action on a specific resource")
        .WithDescription("""
            Evaluates both RBAC/PBAC (permission-based) and ABAC (policy-based) authorization.
            
            Supported resource types:
            - user: Check access to specific user
            - clinic: Check access to specific clinic
            - role: Check access to specific role
            - subscription: Check access to specific subscription
            - patient: Check access to specific patient
            - payment: Check access to specific payment
            - medicalrecord: Check access to specific medical record
            - appointment: Check access to specific appointment
            
            Supported actions: view, edit, delete, create
            
            Returns:
            - allowed: true if user is authorized, false otherwise
            - reason: Why access was denied (null if allowed)
            - policy: Which policy evaluated this (e.g., UserPolicy, ClinicPolicy)
            """)
        .WithOpenApi();
    }
}