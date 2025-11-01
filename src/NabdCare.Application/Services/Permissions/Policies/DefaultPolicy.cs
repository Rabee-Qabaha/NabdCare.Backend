using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Permissions.Policies;

/// <summary>
/// Fallback policy for entities that don't have specific ABAC logic.
/// Always defers to standard RBAC/PBAC checks.
/// </summary>
public class DefaultPolicy<T> : IAccessPolicy<T>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, T entity)
    {
        // If no ABAC logic exists, rely on RBAC + PBAC permissions
        return Task.FromResult(true);
    }
}