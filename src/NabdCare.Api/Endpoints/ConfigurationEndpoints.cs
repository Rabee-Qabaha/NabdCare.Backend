using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NabdCare.Api.Configurations;

namespace NabdCare.Api.Endpoints;

public static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/configuration")
            .WithTags("Configuration")
            .AllowAnonymous(); // Allow frontend to fetch config without auth if needed initially, or secure it. usually secure.
            // Let's secure it by default since it might expose internal policy.

        group.MapGet("/", GetConfiguration)
            .RequireAuthorization();
    }

    private static IResult GetConfiguration(IOptions<SaaSSettings> saasSettings)
    {
        return Results.Ok(new
        {
            FunctionalCurrency = saasSettings.Value.FunctionalCurrency
        });
    }
}
