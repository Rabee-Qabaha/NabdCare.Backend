using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation; // Needed for ValidationException
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions; // Needed for DomainException
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
            // Only log 500s as Errors. Business logic errors (400) are Warnings/Info.
            if (ex is DomainException || ex is ValidationException)
            {
                _logger.LogWarning("Business Rule Exception: {Message}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Unhandled exception: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            }
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Trace-Id"] = traceId;

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // Map the Exception
        var mapping = MapException(exception);

        context.Response.StatusCode = mapping.StatusCode;

        var response = new ApiErrorResponse
        {
            Error = new ErrorResponseDto
            {
                Message = mapping.Message,
                Code = mapping.Code,
                Type = mapping.Type,
                StatusCode = mapping.StatusCode,
                TraceId = traceId,
                Details = mapping.Details, // ✅ Pass field errors to frontend
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

    private static (int StatusCode, string Message, string Code, string Type, Dictionary<string, string[]>? Details) MapException(Exception exception)
    {
        // 1. FluentValidation Errors (Automatic from Validator classes)
        if (exception is ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => JsonNamingPolicy.CamelCase.ConvertName(g.Key), 
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return (
                StatusCodes.Status400BadRequest, 
                "Validation failed", 
                ErrorCodes.VALIDATION_ERROR, 
                "ValidationError", 
                errors
            );
        }

        // 2. Domain Exceptions (Manual Business Rules)
        if (exception is DomainException domainEx)
        {
            Dictionary<string, string[]>? details = null;
            
            // If the error targets a specific field (e.g., TargetField="Name"), map it so frontend highlights the input
            if (!string.IsNullOrEmpty(domainEx.TargetField))
            {
                details = new Dictionary<string, string[]> 
                { 
                    { JsonNamingPolicy.CamelCase.ConvertName(domainEx.TargetField), new[] { domainEx.Message } } 
                };
            }

            return (
                StatusCodes.Status400BadRequest, 
                domainEx.Message, 
                domainEx.ErrorCode, 
                "BusinessRuleViolation", 
                details
            );
        }

        // 3. Standard System Exceptions
        return exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication failed.", ErrorCodes.UNAUTHORIZED, "Unauthorized", null),
            
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message, ErrorCodes.NOT_FOUND, "NotFound", null),
            
            // Catch generic "Already Exists" if not using DomainException
            InvalidOperationException when exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) 
                => (StatusCodes.Status409Conflict, exception.Message, ErrorCodes.DUPLICATE_RESOURCE, "Conflict", null),
            
            // Generic Invalid Operation
            InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message, ErrorCodes.INVALID_OPERATION, "BadRequest", null),
            
            // 500 Fallback
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", ErrorCodes.INTERNAL_ERROR, "InternalServerError", null)
        };
    }
}