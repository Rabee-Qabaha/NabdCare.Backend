using NabdCare.Api.Middleware;

namespace NabdCare.Api.Extensions;

public static class PermissionMiddlewareExtensions
{
    public static IApplicationBuilder UsePermission(this IApplicationBuilder builder, string permission)
    {
        return builder.UseMiddleware<PermissionMiddleware>(permission);
    }
}