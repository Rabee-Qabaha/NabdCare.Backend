namespace NabdCare.Application.Common;

/// <summary>
/// Provides request-scoped information about the current authenticated user
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The ID of the clinic the current user belongs to (null for SuperAdmin)
    /// </summary>
    Guid? ClinicId { get; }

    /// <summary>
    /// The ID of the currently authenticated user
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The email of the currently authenticated user
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Whether the current user is a SuperAdmin
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// The role of the current user
    /// </summary>
    string? UserRole { get; }

    /// <summary>
    /// Whether there is an authenticated user in the current request
    /// </summary>
    bool IsAuthenticated { get; }
}