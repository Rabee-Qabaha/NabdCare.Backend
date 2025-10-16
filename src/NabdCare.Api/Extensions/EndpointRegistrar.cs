using System.Reflection;

namespace NabdCare.Api.Extensions;

public static class EndpointRegistrar
{
    public static void MapAllEndpoints(this IEndpointRouteBuilder builder)
    {
        var methods = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsSealed && t.IsAbstract && t.IsPublic)
            .Where(t => t.Namespace == "NabdCare.Api.Endpoints") // only endpoints
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name.StartsWith("Map") 
                        && m.GetParameters().Length == 1
                        && m.Name != nameof(MapAllEndpoints));

        foreach (var method in methods)
        {
            method.Invoke(null, new object[] { builder });
        }
    }
}