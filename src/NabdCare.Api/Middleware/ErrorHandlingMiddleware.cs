using System.Net;
using System.Text.Json;

namespace NabdCare.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var traceId = Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Trace-Id"] = traceId;

        var isDevelopment = 
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // ✅ P0 FIX: Enhanced exception handling with generic messages for production
        var (statusCode, message, exceptionType) = exception switch
        {
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                isDevelopment ? exception.Message : "Authentication failed.",
                "UnauthorizedAccess"
            ),
            ArgumentException => (
                (int)HttpStatusCode.BadRequest,
                isDevelopment ? exception.Message : "Invalid request.",
                "BadRequest"
            ),
            InvalidOperationException => (
                (int)HttpStatusCode.InternalServerError,
                isDevelopment ? exception.Message : "An error occurred processing your request.",
                "InvalidOperation"
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                isDevelopment ? exception.Message : "An unexpected error occurred.",
                "InternalError"
            )
        };

        var errorResponse = new
        {
            error = new
            {
                message,
                type = exceptionType,
                statusCode,
                traceId,
                // ✅ Only include stack trace in development
                stackTrace = isDevelopment ? exception.StackTrace : null
            }
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        }));
    }
}