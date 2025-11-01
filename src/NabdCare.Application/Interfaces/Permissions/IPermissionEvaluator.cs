using NabdCare.Application.Common;

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

    /// <summary>
    /// Used for ABAC-based query filtering (bulk filtering of roles).
    /// </summary>
    IQueryable<TResource> FilterRoles<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext);

    /// <summary>
    /// Used for ABAC-based query filtering (bulk filtering of users).
    /// </summary>
    IQueryable<TResource> FilterUsers<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext);

    /// <summary>
    /// (Optional) For future: filtering clinics by tenant visibility.
    /// </summary>
    IQueryable<TResource> FilterClinics<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext);

    /// <summary>
    /// (Optional) For future: filtering subscriptions by tenant visibility.
    /// </summary>
    IQueryable<TResource> FilterSubscriptions<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext);
}