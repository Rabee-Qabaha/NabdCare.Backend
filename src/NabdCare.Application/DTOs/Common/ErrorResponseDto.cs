using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Common;

/// <summary>
/// Standardized error response returned by all API endpoints.
/// </summary>
[ExportTsClass]
public class ErrorResponseDto
{
    /// <summary>
    /// Human-readable error message.
    /// Safe to show to users.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code.
    /// Use this for programmatic error handling on frontend.
    /// Values defined in Common.Constants.ErrorCodes
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Exception type name (e.g., "ValidationException", "DomainException").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Unique trace ID for this request.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Field-specific error details.
    /// Key = Field Name (camelCase), Value = List of error messages.
    /// 
    /// Example: 
    /// { 
    ///   "email": ["Invalid email format"], 
    ///   "password": ["Too short", "Must contain number"] 
    /// }
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Details { get; set; } // ✅ CHANGED from object? to Dictionary

    /// <summary>
    /// Full stack trace (development only).
    /// </summary>
    [JsonPropertyName("stackTrace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; set; }
}

/// <summary>
/// Wrapper for error response.
/// </summary>
public class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorResponseDto Error { get; set; } = new();
}

/// <summary>
/// Generic success response wrapper.
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