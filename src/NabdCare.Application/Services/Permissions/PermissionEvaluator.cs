using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;

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
        // step 1: RBAC/PBAC
        if (!await HasAsync(permission))
            return false;

        // step 2: ABAC (optional)
        if (_tenant.IsSuperAdmin) return true;
        if (resource is null) return true; // nothing to evaluate

        var policy = _sp.GetService(typeof(IAccessPolicy<TResource>)) as IAccessPolicy<TResource>;
        if (policy is null)
        {
            // No policy registered for this resource type => allow
            return true;
        }

        var abac = await policy.EvaluateAsync(_tenant, action, resource);
        _logger.LogDebug("ABAC for {ResourceType} action {Action} => {Result}", typeof(TResource).Name, action, abac);
        return abac;
    }
}