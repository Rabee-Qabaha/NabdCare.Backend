using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Permissions;

public class PermissionEvaluator : IPermissionEvaluator
{
    private readonly ITenantContext _tenant;
    private readonly IPermissionService _permissions;
    private readonly IServiceProvider _sp;
    private readonly ILogger<PermissionEvaluator> _logger;

    public PermissionEvaluator(
        ITenantContext tenant,
        IPermissionService permissions,
        IServiceProvider sp,
        ILogger<PermissionEvaluator> logger)
    {
        _tenant = tenant;
        _permissions = permissions;
        _sp = sp;
        _logger = logger;
    }

    // ============================================================
    // 🔹 Core Permission Checks
    // ============================================================

    public async Task<bool> HasAsync(string permission)
    {
        if (_tenant.IsSuperAdmin) return true;
        if (!_tenant.IsAuthenticated || !_tenant.UserId.HasValue || !_tenant.RoleId.HasValue)
            return false;

        var ok = await _permissions.UserHasPermissionAsync(_tenant.UserId.Value, _tenant.RoleId.Value, permission);
        _logger.LogDebug("Has({Permission}) => {Result} (user {UserId}, role {RoleId})",
            permission, ok, _tenant.UserId, _tenant.RoleId);
        return ok;
    }

    public async Task<bool> CanAsync<TResource>(string permission, string action, TResource resource)
    {
        // Step 1: RBAC / PBAC
        if (!await HasAsync(permission))
            return false;

        // Step 2: ABAC (optional)
        if (_tenant.IsSuperAdmin) return true;
        if (resource is null) return true; // Nothing to evaluate

        var policy = _sp.GetService(typeof(IAccessPolicy<TResource>)) as IAccessPolicy<TResource>;
        if (policy is null)
        {
            // No ABAC policy registered for this resource type → allow
            return true;
        }

        var abac = await policy.EvaluateAsync(_tenant, action, resource);
        _logger.LogDebug("ABAC for {ResourceType} action {Action} => {Result}", typeof(TResource).Name, action, abac);
        return abac;
    }

    // ============================================================
    // 🔹 ABAC Query Filters
    // ============================================================

    public IQueryable<TResource> FilterRoles<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (_tenant.IsSuperAdmin)
            return query; // SuperAdmin sees all roles

        if (typeof(TResource) == typeof(Role))
        {
            var filtered = query.Cast<Role>()
                .Where(r => r.ClinicId == _tenant.ClinicId)
                .Cast<TResource>();
            return filtered;
        }

        return query;
    }

    public IQueryable<TResource> FilterUsers<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (_tenant.IsSuperAdmin)
            return query; // SuperAdmin sees all users

        if (typeof(TResource) == typeof(User))
        {
            var clinicId = _tenant.ClinicId;
            if (clinicId.HasValue)
            {
                var filtered = query.Cast<User>()
                    .Where(u => u.ClinicId == clinicId.Value)
                    .Cast<TResource>();
                return filtered;
            }

            // No clinic context → no access
            return Enumerable.Empty<TResource>().AsQueryable();
        }

        return query;
    }

    public IQueryable<TResource> FilterClinics<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (_tenant.IsSuperAdmin)
            return query; // SuperAdmin sees all clinics

        if (typeof(TResource) == typeof(Clinic))
        {
            // Non-superadmins can only see their own clinic
            var filtered = query.Cast<Clinic>()
                .Where(c => c.Id == _tenant.ClinicId)
                .Cast<TResource>();
            return filtered;
        }

        return query;
    }

    public IQueryable<TResource> FilterSubscriptions<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (_tenant.IsSuperAdmin)
            return query; // SuperAdmin sees all subscriptions

        if (typeof(TResource) == typeof(Subscription))
        {
            var clinicId = _tenant.ClinicId;
            if (clinicId.HasValue)
            {
                var filtered = query.Cast<Subscription>()
                    .Where(s => s.ClinicId == clinicId.Value)
                    .Cast<TResource>();
                return filtered;
            }

            return Enumerable.Empty<TResource>().AsQueryable();
        }

        return query;
    }
}