namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionEvaluator
{
    /// <summary>
    /// Pure RBAC/PBAC decision (no resource).
    /// SuperAdmin bypass is baked in.
    /// </summary>
    Task<bool> HasAsync(string permission);

    /// <summary>
    /// RBAC/PBAC + ABAC (resource-aware).
    /// `action` is usually the verb part of your permission, e.g. "View", "Edit".
    /// </summary>
    Task<bool> CanAsync<TResource>(string permission, string action, TResource resource);
}