namespace NabdCare.Api.Endpoints;

public static class DebugEndpoints
{
    public static void MapDebugEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("debug").WithTags("Debug");

        g.MapGet("/whoami", (HttpContext ctx) =>
            {
                var user = ctx.User;
                var claims = user?.Claims.Select(c => new { c.Type, c.Value }) ?? Enumerable.Empty<object>();
                return Results.Json(new
                {
                    IsAuthenticated = user?.Identity?.IsAuthenticated ?? false,
                    AuthenticationType = user?.Identity?.AuthenticationType,
                    Claims = claims
                });
            })
            .RequireAuthorization();
    }
}