using NabdCare.Application.DTOs.Authorizations;

namespace NabdCare.Application.Interfaces.Authorizations;

/// <summary>
/// Service for checking authorization on specific resources.
/// Combines RBAC/PBAC (permission checks) with ABAC (policy evaluation).
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if current user is authorized to perform an action on a specific resource.
    /// 
    /// Process:
    /// 1. Validates request parameters
    /// 2. Checks RBAC/PBAC permissions
    /// 3. Loads the resource
    /// 4. Evaluates ABAC policy
    /// 5. Returns authorization result with reason and policy name
    /// 
    /// Results are cached for 5 minutes.
    /// </summary>
    /// <param name="resourceType">Type of resource: "user", "clinic", "role", "subscription", etc.</param>
    /// <param name="resourceId">Resource ID as string (will be parsed to Guid)</param>
    /// <param name="action">Action to check: "view", "edit", "delete", "create"</param>
    /// <returns>Authorization result with allowed status, reason, and policy name</returns>
    Task<AuthorizationResultDto> CheckAuthorizationAsync(string resourceType, string resourceId, string action);
}