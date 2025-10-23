using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Api.Extensions;

/// <summary>
/// Extension methods for requiring permissions on endpoints.
/// Uses endpoint filters for per-route permission checking.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 18:03:22 UTC
/// </summary>
public static class PermissionEndpointExtensions
{
    /// <summary>
    /// Requires the authenticated user to have the specified permission.
    /// Returns 401 if user is not authenticated, 403 if permission is denied.
    /// </summary>
    /// <param name="builder">The route handler builder</param>
    /// <param name="permission">Required permission (e.g., "Clinics.Create")</param>
    /// <returns>The route handler builder for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when permission is null or empty</exception>
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));

        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;

            // Get services
            var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userContext = httpContext.RequestServices.GetRequiredService<IUserContext>();
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<IPermissionService>>();

            // Validate user authentication
            var userIdStr = userContext.GetCurrentUserId();
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                logger.LogWarning("Permission check failed: User not authenticated for permission {Permission}", permission);
                return Results.Json(
                    new { Error = "User not authenticated" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Get role ID from claims
            var roleIdClaim = httpContext.User.FindFirst("RoleId")?.Value;
            if (string.IsNullOrEmpty(roleIdClaim) || !Guid.TryParse(roleIdClaim, out var roleId))
            {
                logger.LogWarning("Permission check failed: Invalid role claim for user {UserId}, permission {Permission}", 
                    userId, permission);
                return Results.Json(
                    new { Error = "Invalid role claim" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Check permission
            var hasPermission = await permissionService.UserHasPermissionAsync(userId, roleId, permission);
            if (!hasPermission)
            {
                logger.LogWarning("Permission denied: User {UserId} (Role {RoleId}) attempted to access {Permission}", 
                    userId, roleId, permission);
                return Results.Json(
                    new { Error = $"Missing required permission: {permission}" },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            // Permission granted
            logger.LogDebug("Permission granted: User {UserId} accessed {Permission}", userId, permission);
            return await next(context);
        });
    }
}