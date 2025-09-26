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

        // You need to get user's role from the database or from context (simplified example)
        var role = (UserRole)Enum.Parse(typeof(UserRole), context.User.FindFirst("role")?.Value ?? "User");

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