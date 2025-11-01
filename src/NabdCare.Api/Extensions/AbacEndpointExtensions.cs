using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Api.Extensions;

/// <summary>
/// Extension methods to simplify applying ABAC (Attribute-Based Access Control)
/// checks to minimal API endpoints.
/// 
/// Author: Rabee-Qabaha
/// Updated: 2025-10-30
/// </summary>
public static class AbacEndpointExtensions
{
    /// <summary>
    /// Automatically performs ABAC (policy-based) validation for a given resource type.
    /// This version inspects the returned result (e.g., Results.Ok(resource)).
    /// </summary>
    /// <typeparam name="TResource">The type of the resource being checked (e.g., User, Clinic)</typeparam>
    /// <param name="builder">The route builder</param>
    /// <param name="permission">Permission name, e.g. "Users.Edit"</param>
    /// <param name="action">Action name, e.g. "edit", "view"</param>
    /// <param name="resourceSelector">Function to extract the resource object from the request result</param>
    /// <returns>The route handler builder for chaining</returns>
    public static RouteHandlerBuilder WithAbac<TResource>(
        this RouteHandlerBuilder builder,
        string permission,
        string action,
        Func<object?, TResource?> resourceSelector)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));

        return builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var logger = http.RequestServices.GetRequiredService<ILogger<IPermissionEvaluator>>();
            var evaluator = http.RequestServices.GetRequiredService<IPermissionEvaluator>();

            // Execute handler first to capture the resource
            var result = await next(context);

            if (result is IResult r)
            {
                // Try extracting "Value" property (e.g., from Results.Ok(resource))
                var valueProperty = r.GetType().GetProperty("Value");
                var resource = valueProperty != null ? valueProperty.GetValue(r) : null;
                var typedResource = resourceSelector(resource);

                if (typedResource != null)
                {
                    var allowed = await evaluator.CanAsync(permission, action, typedResource);
                    if (!allowed)
                    {
                        logger.LogWarning("ABAC denied access for permission {Permission} on {ResourceType}",
                            permission, typeof(TResource).Name);
                        return Results.Json(
                            new { error = "Access denied by ABAC policy" },
                            statusCode: StatusCodes.Status403Forbidden);
                    }
                }
            }

            return result;
        });
    }

    /// <summary>
    /// ABAC validation using an asynchronous resource resolver — ideal for endpoints
    /// that don’t directly return the resource (e.g., DELETE, PATCH).
    /// </summary>
    /// <typeparam name="TResource">The type of the resource being checked</typeparam>
    /// <param name="builder">The route handler builder</param>
    /// <param name="permission">Permission name, e.g., "Users.Delete"</param>
    /// <param name="action">Action name, e.g., "delete"</param>
    /// <param name="resourceResolver">Function that loads the resource from the database using HttpContext</param>
    /// <returns>The route handler builder for chaining</returns>
    public static RouteHandlerBuilder WithAbac<TResource>(
        this RouteHandlerBuilder builder,
        string permission,
        string action,
        Func<HttpContext, Task<TResource?>> resourceResolver)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));

        return builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var logger = http.RequestServices.GetRequiredService<ILogger<IPermissionEvaluator>>();
            var evaluator = http.RequestServices.GetRequiredService<IPermissionEvaluator>();

            // Resolve resource before executing the handler (pre-authorization)
            var resource = await resourceResolver(http);
            if (resource is not null)
            {
                var allowed = await evaluator.CanAsync(permission, action, resource);
                if (!allowed)
                {
                    logger.LogWarning("ABAC pre-check denied access for permission {Permission} on {ResourceType}",
                        permission, typeof(TResource).Name);
                    return Results.Json(
                        new { error = "Access denied by ABAC policy" },
                        statusCode: StatusCodes.Status403Forbidden);
                }
            }

            // If allowed, continue executing handler
            return await next(context);
        });
    }
}