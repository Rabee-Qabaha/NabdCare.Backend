using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Extensions;

public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userContext = httpContext.RequestServices.GetRequiredService<IUserContext>();

            var userIdStr = userContext.GetCurrentUserId();
            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var roleClaim = httpContext.User.FindFirst("role")?.Value ?? "User";
            if (!Enum.TryParse<UserRole>(roleClaim, true, out var role))
                return Results.Unauthorized();

            var hasPermission = await permissionService.UserHasPermissionAsync(userId, role, permission);
            if (!hasPermission)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            return await next(context);
        });
    }
}