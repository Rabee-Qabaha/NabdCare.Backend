using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Common;

/// <summary>
/// Standardized error response returned by all API endpoints.
/// 
/// Example JSON:
/// {
///   "error": {
///     "message": "Email already in use",
///     "code": "DUPLICATE_EMAIL",
///     "type": "Conflict",
///     "statusCode": 409,
///     "traceId": "a1b2c3d4e5f6g7h8",
///     "details": null,
///     "stackTrace": null
///   }
/// }
/// </summary>
[ExportTsClass]
public class ErrorResponseDto
{
    /// <summary>
    /// Human-readable error message.
    /// Safe to show to users.
    /// Example: "Email already in use"
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code.
    /// Use this for programmatic error handling on frontend.
    /// Example: "DUPLICATE_EMAIL"
    /// 
    /// Values defined in Common.Constants.ErrorCodes
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Exception type name.
    /// Used for categorization and logging.
    /// Example: "InvalidOperationException"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code.
    /// Example: 409
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Unique trace ID for this request.
    /// Use this when contacting support to find logs.
    /// Format: GUID without hyphens
    /// Example: "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"
    /// </summary>
    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (development only).
    /// 
    /// Examples:
    /// - Validation errors: { "validationErrors": { "email": ["Invalid format"] } }
    /// - Original exception: { "originalMessage": "..." }
    /// - Custom data: { "userId": "...", "reason": "..." }
    /// 
    /// Null in production.
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; set; }

    /// <summary>
    /// Full stack trace (development only).
    /// Null in production for security.
    /// </summary>
    [JsonPropertyName("stackTrace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; set; }
}

/// <summary>
/// Wrapper for error response.
/// All error responses follow this structure.
/// </summary>
public class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorResponseDto Error { get; set; } = new();
}

/// <summary>
/// Generic success response wrapper (optional, for consistency).
/// Use this if you want uniform response structure for success too.
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;
}