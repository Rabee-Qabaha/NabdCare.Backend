using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Middleware;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _permission;

    public PermissionMiddleware(RequestDelegate next, string permission)
    {
        _next = next;
        _permission = permission;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext, IPermissionService permissionService)
    {
        var userIdStr = userContext.GetCurrentUserId();

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("User not authenticated");
            return;
        }

        // Get role from claims
        var roleClaim = context.User.FindFirst("role")?.Value ?? "User";
        if (!Enum.TryParse<UserRole>(roleClaim, true, out var role))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid role claim");
            return;
        }

        // Check if the user has the required permission
        var hasPermission = await permissionService.UserHasPermissionAsync(userId, role, _permission);
        if (!hasPermission)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden: insufficient permissions");
            return;
        }

        await _next(context);
    }
}