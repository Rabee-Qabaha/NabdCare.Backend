using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Domain.Entities.Subscriptions;

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
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HasAsync(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Attempt to check empty permission. Error code: {ErrorCode}", 
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission name cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin has permission {Permission}", permission);
            return true;
        }

        if (!_tenant.IsAuthenticated)
        {
            _logger.LogDebug("Unauthenticated user checking permission {Permission}. Error code: {ErrorCode}", 
                permission, ErrorCodes.UNAUTHORIZED);
            return false;
        }

        if (!_tenant.UserId.HasValue)
        {
            _logger.LogWarning("User ID not set for authenticated user. Error code: {ErrorCode}", 
                ErrorCodes.UNAUTHORIZED);
            return false;
        }

        if (!_tenant.RoleId.HasValue)
        {
            _logger.LogWarning("Role ID not set for user {UserId}. Error code: {ErrorCode}", 
                _tenant.UserId, ErrorCodes.UNAUTHORIZED);
            return false;
        }

        var ok = await _permissions.UserHasPermissionAsync(
            _tenant.UserId.Value, 
            _tenant.RoleId.Value, 
            permission);

        _logger.LogDebug("Permission check - Has({Permission}) => {Result} (user {UserId}, role {RoleId})",
            permission, ok, _tenant.UserId, _tenant.RoleId);

        if (!ok)
        {
            _logger.LogWarning("User {UserId} does not have permission {Permission}. Error code: {ErrorCode}",
                _tenant.UserId, permission, ErrorCodes.INSUFFICIENT_PERMISSIONS);
        }

        return ok;
    }

    public async Task<bool> CanAsync<TResource>(string permission, string action, TResource resource)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Attempt to check empty permission for action {Action}. Error code: {ErrorCode}",
                action, ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission name cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            _logger.LogWarning("Attempt to check permission {Permission} with empty action. Error code: {ErrorCode}",
                permission, ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Action name cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(action));
        }

        _logger.LogDebug("Checking permission {Permission} for action {Action} on resource {ResourceType}",
            permission, action, typeof(TResource).Name);

        // Step 1: RBAC / PBAC
        if (!await HasAsync(permission))
        {
            _logger.LogWarning("User lacks permission {Permission} for action {Action}. Error code: {ErrorCode}",
                permission, action, ErrorCodes.INSUFFICIENT_PERMISSIONS);
            return false;
        }

        // Step 2: SuperAdmin bypass
        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin bypassing ABAC for {ResourceType}", typeof(TResource).Name);
            return true;
        }

        // Step 3: No resource to evaluate
        if (resource is null)
        {
            _logger.LogDebug("No resource to evaluate ABAC for action {Action}", action);
            return true;
        }

        // Step 4: ABAC evaluation
        var policy = _sp.GetService(typeof(IAccessPolicy<TResource>)) as IAccessPolicy<TResource>;
        if (policy is null)
        {
            _logger.LogDebug("No ABAC policy registered for {ResourceType} - allowing access", 
                typeof(TResource).Name);
            return true;
        }

        var abac = await policy.EvaluateAsync(_tenant, action, resource);
        
        _logger.LogDebug("ABAC evaluation for {ResourceType} action {Action} => {Result}",
            typeof(TResource).Name, action, abac);

        if (!abac)
        {
            _logger.LogWarning("ABAC denied action {Action} on {ResourceType}. Error code: {ErrorCode}",
                action, typeof(TResource).Name, ErrorCodes.FORBIDDEN);
        }

        return abac;
    }

    public IQueryable<TResource> FilterRoles<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (query == null)
            throw new ArgumentNullException($"Query cannot be null. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(query));

        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Empty permission provided to FilterRoles. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        _logger.LogDebug("Filtering roles for user with permission {Permission}", permission);

        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin accessing all roles - no filtering applied");
            return query;
        }

        if (typeof(TResource) == typeof(Role))
        {
            _logger.LogDebug("Applying clinic filter to roles query. Clinic: {ClinicId}", _tenant.ClinicId);

            var filtered = query.Cast<Role>()
                .Where(r => r.ClinicId == _tenant.ClinicId)
                .Cast<TResource>();

            return filtered;
        }

        _logger.LogDebug("Resource type {ResourceType} is not Role - returning unfiltered query", 
            typeof(TResource).Name);
        return query;
    }

    public IQueryable<TResource> FilterUsers<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (query == null)
            throw new ArgumentNullException($"Query cannot be null. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(query));

        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Empty permission provided to FilterUsers. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        _logger.LogDebug("Filtering users for user with permission {Permission}", permission);

        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin accessing all users - no filtering applied");
            return query;
        }

        if (typeof(TResource) == typeof(User))
        {
            var clinicId = _tenant.ClinicId;
            
            if (!clinicId.HasValue)
            {
                _logger.LogWarning("Non-SuperAdmin user has no clinic context - returning empty result. Error code: {ErrorCode}",
                    ErrorCodes.FORBIDDEN);
                return Enumerable.Empty<TResource>().AsQueryable();
            }

            _logger.LogDebug("Applying clinic filter to users query. Clinic: {ClinicId}", clinicId);

            var filtered = query.Cast<User>()
                .Where(u => u.ClinicId == clinicId.Value)
                .Cast<TResource>();

            return filtered;
        }

        _logger.LogDebug("Resource type {ResourceType} is not User - returning unfiltered query", 
            typeof(TResource).Name);
        return query;
    }

    public IQueryable<TResource> FilterClinics<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (query == null)
            throw new ArgumentNullException($"Query cannot be null. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(query));

        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Empty permission provided to FilterClinics. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        _logger.LogDebug("Filtering clinics for user with permission {Permission}", permission);

        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin accessing all clinics - no filtering applied");
            return query;
        }

        if (typeof(TResource) == typeof(Clinic))
        {
            if (!_tenant.ClinicId.HasValue)
            {
                _logger.LogWarning("Non-SuperAdmin user has no clinic context - returning empty result. Error code: {ErrorCode}",
                    ErrorCodes.FORBIDDEN);
                return Enumerable.Empty<TResource>().AsQueryable();
            }

            _logger.LogDebug("Applying clinic filter to clinics query. Clinic: {ClinicId}", _tenant.ClinicId);

            var filtered = query.Cast<Clinic>()
                .Where(c => c.Id == _tenant.ClinicId.Value)
                .Cast<TResource>();

            return filtered;
        }

        _logger.LogDebug("Resource type {ResourceType} is not Clinic - returning unfiltered query", 
            typeof(TResource).Name);
        return query;
    }

    public IQueryable<TResource> FilterSubscriptions<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (query == null)
            throw new ArgumentNullException($"Query cannot be null. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(query));

        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Empty permission provided to FilterSubscriptions. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        _logger.LogDebug("Filtering subscriptions for user with permission {Permission}", permission);

        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin accessing all subscriptions - no filtering applied");
            return query;
        }

        if (typeof(TResource) == typeof(Subscription))
        {
            var clinicId = _tenant.ClinicId;

            if (!clinicId.HasValue)
            {
                _logger.LogWarning("Non-SuperAdmin user has no clinic context - returning empty result. Error code: {ErrorCode}",
                    ErrorCodes.FORBIDDEN);
                return Enumerable.Empty<TResource>().AsQueryable();
            }

            _logger.LogDebug("Applying clinic filter to subscriptions query. Clinic: {ClinicId}", clinicId);

            var filtered = query.Cast<Subscription>()
                .Where(s => s.ClinicId == clinicId.Value)
                .Cast<TResource>();

            return filtered;
        }

        _logger.LogDebug("Resource type {ResourceType} is not Subscription - returning unfiltered query", 
            typeof(TResource).Name);
        return query;
    }

    // ✅ ADDED: Invoice Filtering (ABAC)
    public IQueryable<TResource> FilterInvoices<TResource>(
        IQueryable<TResource> query,
        string permission,
        IUserContext userContext)
    {
        if (query == null)
            throw new ArgumentNullException($"Query cannot be null. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(query));

        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogWarning("Empty permission provided to FilterInvoices. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permission));
        }

        _logger.LogDebug("Filtering invoices for user with permission {Permission}", permission);

        if (_tenant.IsSuperAdmin)
        {
            _logger.LogDebug("SuperAdmin accessing all invoices - no filtering applied");
            return query;
        }

        if (typeof(TResource) == typeof(Invoice))
        {
            var clinicId = _tenant.ClinicId;

            if (!clinicId.HasValue)
            {
                _logger.LogWarning("Non-SuperAdmin user has no clinic context - returning empty result. Error code: {ErrorCode}",
                    ErrorCodes.FORBIDDEN);
                return Enumerable.Empty<TResource>().AsQueryable();
            }

            _logger.LogDebug("Applying clinic filter to invoices query. Clinic: {ClinicId}", clinicId);

            var filtered = query.Cast<Invoice>()
                .Where(i => i.ClinicId == clinicId.Value)
                .Cast<TResource>();

            return filtered;
        }

        _logger.LogDebug("Resource type {ResourceType} is not Invoice - returning unfiltered query", 
            typeof(TResource).Name);
        return query;
    }
}