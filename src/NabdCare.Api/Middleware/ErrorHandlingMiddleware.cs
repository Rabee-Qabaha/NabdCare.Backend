using System.Text.Json;
using System.Text.Json.Serialization;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Common;

namespace NabdCare.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// 
/// Responsibilities:
/// - Catches all unhandled exceptions
/// - Maps exceptions to HTTP status codes
/// - Returns consistent error response format
/// - Logs errors for debugging
/// - Hides sensitive info in production
/// 
/// Error Response Format:
/// {
///   "error": {
///     "message": "...",
///     "code": "ERROR_CODE",
///     "type": "ExceptionType",
///     "statusCode": 500,
///     "traceId": "...",
///     "details": null,
///     "stackTrace": null
///   }
/// }
/// </summary>
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
            _logger.LogError(ex, 
                "Unhandled exception: {ExceptionType} - {Message}", 
                ex.GetType().Name, ex.Message);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Converts exception to standardized error response.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var traceId = Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Trace-Id"] = traceId;

        var isDevelopment = 
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // ✅ Map exception to error details
        var errorDetails = MapExceptionToErrorDetails(exception, isDevelopment);

        context.Response.StatusCode = errorDetails.StatusCode;

        var errorResponse = new ApiErrorResponse
        {
            Error = new ErrorResponseDto
            {
                Message = errorDetails.Message,
                Code = errorDetails.Code,
                Type = errorDetails.Type,
                StatusCode = errorDetails.StatusCode,
                TraceId = traceId,
                Details = isDevelopment ? errorDetails.Details : null,
                StackTrace = isDevelopment ? exception.StackTrace : null
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = isDevelopment
        };

        await context.Response.WriteAsJsonAsync(errorResponse, options);
    }

    /// <summary>
    /// Maps exception types to appropriate error details.
    /// 
    /// Add more exception types here as needed.
    /// </summary>
    private static (int StatusCode, string Message, string Code, string Type, object? Details) 
        MapExceptionToErrorDetails(Exception exception, bool isDevelopment)
    {
        return exception switch
        {
            // ============================================
            // UNAUTHORIZED (401)
            // ============================================
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed. Please log in.",
                ErrorCodes.UNAUTHORIZED,
                "UnauthorizedAccess",
                isDevelopment ? new { OriginalMessage = exception.Message } : null
            ),

            // ============================================
            // VALIDATION / BAD REQUEST (400)
            // ============================================
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request. Please check your input.",
                ErrorCodes.INVALID_ARGUMENT,
                "BadRequest",
                isDevelopment ? new { OriginalMessage = exception.Message } : null
            ),

            // ============================================
            // NOT FOUND (404)
            // ============================================
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "The requested resource was not found.",
                ErrorCodes.NOT_FOUND,
                "NotFound",
                isDevelopment ? new { OriginalMessage = exception.Message } : null
            ),

            // ============================================
            // CONFLICT (409)
            // ============================================
            InvalidOperationException when exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status409Conflict,
                "This resource already exists.",
                ErrorCodes.DUPLICATE_RESOURCE,
                "Conflict",
                isDevelopment ? new { OriginalMessage = exception.Message } : null
            ),

            // ============================================
            // GENERIC INVALID OPERATION (500)
            // ============================================
            InvalidOperationException => (
                StatusCodes.Status500InternalServerError,
                "An error occurred processing your request.",
                ErrorCodes.INVALID_OPERATION,
                "InvalidOperation",
                isDevelopment ? new { OriginalMessage = exception.Message } : null
            ),

            // ============================================
            // DEFAULT: INTERNAL SERVER ERROR (500)
            // ============================================
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.",
                ErrorCodes.INTERNAL_ERROR,
                exception.GetType().Name,
                isDevelopment ? new { OriginalMessage = exception.Message } : null
            )
        };
    }
}