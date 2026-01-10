using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.Common.Constants;

/// <summary>
/// Standard error codes used throughout the API.
/// </summary>
[ExportTsClass(OutputDir = "constants")]
public static class ErrorCodes
{
    // ============================================
    // Authentication (401)
    // ============================================
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string SESSION_EXPIRED = "SESSION_EXPIRED";
    public const string TOKEN_INVALID = "TOKEN_INVALID";
    public const string AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED";

    // ============================================
    // Authorization (403)
    // ============================================
    public const string FORBIDDEN = "FORBIDDEN";
    public const string INSUFFICIENT_PERMISSIONS = "INSUFFICIENT_PERMISSIONS";
    public const string ACCESS_DENIED = "ACCESS_DENIED";

    // ============================================
    // Security (401/403)
    // ============================================
    public const string SECURITY_VIOLATION = "SECURITY_VIOLATION";
    public const string TOKEN_REUSE_DETECTED = "TOKEN_REUSE_DETECTED";
    public const string SUSPICIOUS_ACTIVITY = "SUSPICIOUS_ACTIVITY";
    public const string UNAUTHORIZED_ACCESS_ATTEMPT = "UNAUTHORIZED_ACCESS_ATTEMPT";

    // ============================================
    // Validation (400)
    // ============================================
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INVALID_ARGUMENT = "INVALID_ARGUMENT";
    public const string INVALID_REQUEST = "INVALID_REQUEST";
    public const string INVALID_EMAIL = "INVALID_EMAIL";
    public const string INVALID_PASSWORD = "INVALID_PASSWORD";

    // ============================================
    // Resource (404)
    // ============================================
    public const string NOT_FOUND = "NOT_FOUND";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string CLINIC_NOT_FOUND = "CLINIC_NOT_FOUND";
    public const string ROLE_NOT_FOUND = "ROLE_NOT_FOUND";
    public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";

    // ============================================
    // Conflict (409)
    // ============================================
    public const string CONFLICT = "CONFLICT";
    public const string DUPLICATE_EMAIL = "DUPLICATE_EMAIL";
    public const string DUPLICATE_RESOURCE = "DUPLICATE_RESOURCE";
    public const string RESOURCE_ALREADY_EXISTS = "RESOURCE_ALREADY_EXISTS";
    public const string CONSTRAINT_VIOLATION = "CONSTRAINT_VIOLATION";
    public const string DUPLICATE_NAME = "DUPLICATE_NAME";
    public const string DUPLICATE_SLUG = "DUPLICATE_SLUG";

    // ============================================
    // Rate Limiting (429)
    // ============================================
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
    public const string TOO_MANY_REQUESTS = "TOO_MANY_REQUESTS";

    // ============================================
    // Server (500)
    // ============================================
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string INVALID_OPERATION = "INVALID_OPERATION";
    public const string DATABASE_ERROR = "DATABASE_ERROR";
    public const string OPERATION_FAILED = "OPERATION_FAILED";
    
    // ============================================
    // 💼 Business Logic / Subscription Rules
    // ============================================
    public const string SUBSCRIPTION_REQUIRED = "SUBSCRIPTION_REQUIRED";
    public const string LIMIT_EXCEEDED = "LIMIT_EXCEEDED";
    public const string SUBSCRIPTION_EXPIRED = "SUBSCRIPTION_EXPIRED";
    public const string FEATURE_NOT_ENABLED = "FEATURE_NOT_ENABLED";
    public const string ACCOUNT_SUSPENDED = "ACCOUNT_SUSPENDED";
}