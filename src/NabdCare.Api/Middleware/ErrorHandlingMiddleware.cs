using System.Text.Json;
using System.Text.Json.Serialization;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Common;

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
            _logger.LogError(ex, "Unhandled exception: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Generate Trace ID for debugging
        var traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Trace-Id"] = traceId;

        // Check environment
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // Map Exception
        var (statusCode, message, code, type) = MapException(exception);

        context.Response.StatusCode = statusCode;

        var response = new ApiErrorResponse
        {
            Error = new ErrorResponseDto
            {
                Message = message,
                Code = code,
                Type = type,
                StatusCode = statusCode,
                TraceId = traceId,
                // Only show StackTrace in Dev; 'Details' can be used for specific validation errors if needed
                StackTrace = isDevelopment ? exception.StackTrace : null
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }

    private static (int StatusCode, string Message, string Code, string Type) MapException(Exception exception)
    {
        return exception switch
        {
            // 401: Authentication Issues
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed. You do not have permission to access this resource.",
                ErrorCodes.UNAUTHORIZED,
                "Unauthorized"
            ),

            // 404: Not Found
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                exception.Message, // Safe to show "User not found", "Clinic not found"
                ErrorCodes.NOT_FOUND,
                "NotFound"
            ),

            // 409: Conflicts (Duplicates)
            InvalidOperationException when exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status409Conflict,
                exception.Message, // Show "Email already exists"
                ErrorCodes.DUPLICATE_RESOURCE,
                "Conflict"
            ),

            // ✅ 400: Business Rule Violations (Subscription Limits, Logic Errors)
            // We change this from 500 -> 400 because these are logic checks the user can fix/understand.
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                exception.Message,
                ErrorCodes.INVALID_OPERATION,
                "BusinessRuleViolation"
            ),

            // 400: Bad Arguments
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                exception.Message,
                ErrorCodes.INVALID_ARGUMENT,
                "BadRequest"
            ),

            // 500: Everything else (Database crashes, NullReference, etc.)
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.", // Hide internal details
                ErrorCodes.INTERNAL_ERROR,
                "InternalServerError"
            )
        };
    }
}