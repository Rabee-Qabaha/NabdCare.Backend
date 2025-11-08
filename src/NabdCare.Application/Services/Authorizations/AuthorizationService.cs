using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Authorizations;
using NabdCare.Application.Interfaces.Authorizations;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Authorizations;

public class AuthorizationService : IAuthorizationService
{
    private readonly ITenantContext _tenantContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthorizationService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(2);

    public AuthorizationService(
        ITenantContext tenantContext,
        IPermissionEvaluator permissionEvaluator,
        IAuthorizationRepository authorizationRepository,
        IMemoryCache cache,
        ILogger<AuthorizationService> logger)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
        _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthorizationResultDto> CheckAuthorizationAsync(
        string resourceType,
        string resourceId,
        string action)
    {
        // Normalize inputs
        resourceType = resourceType.ToLower().Trim();
        resourceId = resourceId.Trim();
        action = action.ToLower().Trim();

        // Validate inputs
        if (string.IsNullOrEmpty(resourceType))
        {
            _logger.LogWarning("Authorization check with empty resource type. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            return CreateDeniedResult(resourceType, action, $"Invalid resource type. Error code: {ErrorCodes.INVALID_ARGUMENT}");
        }

        if (string.IsNullOrEmpty(resourceId) || !Guid.TryParse(resourceId, out var resourceGuid))
        {
            _logger.LogWarning("Authorization check with invalid resource ID format. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            return CreateDeniedResult(resourceType, action, $"Invalid resource ID format. Error code: {ErrorCodes.INVALID_ARGUMENT}");
        }

        if (string.IsNullOrEmpty(action))
        {
            _logger.LogWarning("Authorization check with empty action. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            return CreateDeniedResult(resourceType, action, $"Invalid action. Error code: {ErrorCodes.INVALID_ARGUMENT}");
        }

        // Check cache
        var cacheKey = GetCacheKey(resourceGuid, resourceType, action);
        if (_cache.TryGetValue(cacheKey, out AuthorizationResultDto? cached))
        {
            _logger.LogDebug("Cache hit for authorization check: {ResourceType}:{ResourceId}:{Action}",
                resourceType, resourceId, action);
            return cached!;
        }

        _logger.LogDebug("Cache miss for authorization check: {ResourceType}:{ResourceId}:{Action}. Evaluating...",
            resourceType, resourceId, action);

        // Evaluate authorization
        var result = await EvaluateAuthorizationAsync(resourceType, resourceGuid, action);

        // Cache result (cache both allowed and denied results)
        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            SlidingExpiration = SlidingExpiration,
            Size = 1
        });

        return result;
    }

    private async Task<AuthorizationResultDto> EvaluateAuthorizationAsync(
        string resourceType,
        Guid resourceId,
        string action)
    {
        var normalizedType = resourceType.ToLower();

        _logger.LogDebug("Evaluating authorization for {ResourceType}:{ResourceId}:{Action}",
            normalizedType, resourceId, action);

        return normalizedType switch
        {
            "user" => await EvaluateUserAuthorizationAsync(resourceId, action),
            "clinic" => await EvaluateClinicAuthorizationAsync(resourceId, action),
            "role" => await EvaluateRoleAuthorizationAsync(resourceId, action),
            "subscription" => await EvaluateSubscriptionAuthorizationAsync(resourceId, action),
            "payment" => await EvaluatePaymentAuthorizationAsync(resourceId, action),
            _ => CreateDeniedResult(resourceType, action, $"Unknown resource type: {resourceType}. Error code: {ErrorCodes.INVALID_ARGUMENT}")
        };
    }

    private async Task<AuthorizationResultDto> EvaluateUserAuthorizationAsync(Guid userId, string action)
    {
        _logger.LogDebug("Evaluating user authorization: {UserId}:{Action}", userId, action);

        var hasPermission = await _permissionEvaluator.HasAsync("Users.View");
        if (!hasPermission)
        {
            _logger.LogWarning("User {UserId} denied: Missing Users.View permission. Error code: {ErrorCode}",
                userId, ErrorCodes.INSUFFICIENT_PERMISSIONS);
            return CreateDeniedResult("user", action, $"Missing Users.View permission. Error code: {ErrorCodes.INSUFFICIENT_PERMISSIONS}", policy: null);
        }

        var user = await _authorizationRepository.GetUserByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found. Error code: {ErrorCode}",
                userId, ErrorCodes.NOT_FOUND);
            return CreateDeniedResult("user", action, $"User not found. Error code: {ErrorCodes.NOT_FOUND}", policy: null);
        }

        var allowed = await _permissionEvaluator.CanAsync("Users.View", action, user);
        if (!allowed)
        {
            _logger.LogWarning("Access denied by UserPolicy for user {UserId} action {Action} requested by user {CurrentUserId}. Error code: {ErrorCode}",
                userId, action, _tenantContext.UserId, ErrorCodes.FORBIDDEN);
            return CreateDeniedResult("user", action, $"User belongs to a different clinic. Error code: {ErrorCodes.FORBIDDEN}", policy: "UserPolicy");
        }

        _logger.LogInformation("Authorization allowed: User {UserId} action {Action}", userId, action);
        return CreateAllowedResult("user", action);
    }

    private async Task<AuthorizationResultDto> EvaluateClinicAuthorizationAsync(Guid clinicId, string action)
    {
        _logger.LogDebug("Evaluating clinic authorization: {ClinicId}:{Action}", clinicId, action);

        var hasPermission = await _permissionEvaluator.HasAsync("Clinics.ViewAll");
        if (!hasPermission)
        {
            _logger.LogWarning("Clinic {ClinicId} denied: Missing Clinics.ViewAll permission. Error code: {ErrorCode}",
                clinicId, ErrorCodes.INSUFFICIENT_PERMISSIONS);
            return CreateDeniedResult("clinic", action, $"Missing Clinics.ViewAll permission. Error code: {ErrorCodes.INSUFFICIENT_PERMISSIONS}", policy: null);
        }

        var clinic = await _authorizationRepository.GetClinicByIdAsync(clinicId);

        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found. Error code: {ErrorCode}",
                clinicId, ErrorCodes.NOT_FOUND);
            return CreateDeniedResult("clinic", action, $"Clinic not found. Error code: {ErrorCodes.NOT_FOUND}", policy: null);
        }

        var allowed = await _permissionEvaluator.CanAsync("Clinics.ViewAll", action, clinic);
        if (!allowed)
        {
            _logger.LogWarning("Access denied by ClinicPolicy for clinic {ClinicId} action {Action}. Error code: {ErrorCode}",
                clinicId, action, ErrorCodes.FORBIDDEN);
            return CreateDeniedResult("clinic", action, $"Clinic is not accessible. Error code: {ErrorCodes.FORBIDDEN}", policy: "ClinicPolicy");
        }

        _logger.LogInformation("Authorization allowed: Clinic {ClinicId} action {Action}", clinicId, action);
        return CreateAllowedResult("clinic", action);
    }

    private async Task<AuthorizationResultDto> EvaluateRoleAuthorizationAsync(Guid roleId, string action)
    {
        _logger.LogDebug("Evaluating role authorization: {RoleId}:{Action}", roleId, action);

        var hasPermission = await _permissionEvaluator.HasAsync("Roles.View");
        if (!hasPermission)
        {
            _logger.LogWarning("Role {RoleId} denied: Missing Roles.View permission. Error code: {ErrorCode}",
                roleId, ErrorCodes.INSUFFICIENT_PERMISSIONS);
            return CreateDeniedResult("role", action, $"Missing Roles.View permission. Error code: {ErrorCodes.INSUFFICIENT_PERMISSIONS}", policy: null);
        }

        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);

        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}",
                roleId, ErrorCodes.NOT_FOUND);
            return CreateDeniedResult("role", action, $"Role not found. Error code: {ErrorCodes.NOT_FOUND}", policy: null);
        }

        var allowed = await _permissionEvaluator.CanAsync("Roles.View", action, role);
        if (!allowed)
        {
            _logger.LogWarning("Access denied by RolePolicy for role {RoleId} action {Action}. Error code: {ErrorCode}",
                roleId, action, ErrorCodes.FORBIDDEN);
            return CreateDeniedResult("role", action, $"Role is not accessible. Error code: {ErrorCodes.FORBIDDEN}", policy: "RolePolicy");
        }

        _logger.LogInformation("Authorization allowed: Role {RoleId} action {Action}", roleId, action);
        return CreateAllowedResult("role", action);
    }

    private async Task<AuthorizationResultDto> EvaluateSubscriptionAuthorizationAsync(Guid subscriptionId, string action)
    {
        _logger.LogDebug("Evaluating subscription authorization: {SubscriptionId}:{Action}", subscriptionId, action);

        var hasPermission = await _permissionEvaluator.HasAsync("Subscriptions.View");
        if (!hasPermission)
        {
            _logger.LogWarning("Subscription {SubscriptionId} denied: Missing Subscriptions.View permission. Error code: {ErrorCode}",
                subscriptionId, ErrorCodes.INSUFFICIENT_PERMISSIONS);
            return CreateDeniedResult("subscription", action, $"Missing Subscriptions.View permission. Error code: {ErrorCodes.INSUFFICIENT_PERMISSIONS}", policy: null);
        }

        var subscription = await _authorizationRepository.GetSubscriptionByIdAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found. Error code: {ErrorCode}",
                subscriptionId, ErrorCodes.NOT_FOUND);
            return CreateDeniedResult("subscription", action, $"Subscription not found. Error code: {ErrorCodes.NOT_FOUND}", policy: null);
        }

        var allowed = await _permissionEvaluator.CanAsync("Subscriptions.View", action, subscription);
        if (!allowed)
        {
            _logger.LogWarning("Access denied by SubscriptionPolicy for subscription {SubscriptionId} action {Action}. Error code: {ErrorCode}",
                subscriptionId, action, ErrorCodes.FORBIDDEN);
            return CreateDeniedResult("subscription", action, $"Subscription belongs to a different clinic. Error code: {ErrorCodes.FORBIDDEN}", policy: "SubscriptionPolicy");
        }

        _logger.LogInformation("Authorization allowed: Subscription {SubscriptionId} action {Action}", subscriptionId, action);
        return CreateAllowedResult("subscription", action);
    }

    private async Task<AuthorizationResultDto> EvaluatePaymentAuthorizationAsync(Guid paymentId, string action)
    {
        _logger.LogDebug("Evaluating payment authorization: {PaymentId}:{Action}", paymentId, action);

        var hasPermission = await _permissionEvaluator.HasAsync("Payments.View");
        if (!hasPermission)
        {
            _logger.LogWarning("Payment {PaymentId} denied: Missing Payments.View permission. Error code: {ErrorCode}",
                paymentId, ErrorCodes.INSUFFICIENT_PERMISSIONS);
            return CreateDeniedResult("payment", action, $"Missing Payments.View permission. Error code: {ErrorCodes.INSUFFICIENT_PERMISSIONS}", policy: null);
        }

        var payment = await _authorizationRepository.GetPaymentByIdAsync(paymentId);

        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found. Error code: {ErrorCode}",
                paymentId, ErrorCodes.NOT_FOUND);
            return CreateDeniedResult("payment", action, $"Payment not found. Error code: {ErrorCodes.NOT_FOUND}", policy: null);
        }

        var allowed = await _permissionEvaluator.CanAsync("Payments.View", action, payment);
        if (!allowed)
        {
            _logger.LogWarning("Access denied by PaymentPolicy for payment {PaymentId} action {Action}. Error code: {ErrorCode}",
                paymentId, action, ErrorCodes.FORBIDDEN);
            return CreateDeniedResult("payment", action, $"Payment belongs to a different clinic. Error code: {ErrorCodes.FORBIDDEN}", policy: "PaymentPolicy");
        }

        _logger.LogInformation("Authorization allowed: Payment {PaymentId} action {Action}", paymentId, action);
        return CreateAllowedResult("payment", action);
    }

    private string GetCacheKey(Guid resourceId, string resourceType, string action)
        => $"auth:check:{_tenantContext.UserId}:{resourceType}:{resourceId}:{action}";

    private AuthorizationResultDto CreateAllowedResult(string resourceType, string action)
        => new()
        {
            Allowed = true,
            Reason = null,
            Policy = null,
            ResourceType = resourceType,
            Action = action
        };

    private AuthorizationResultDto CreateDeniedResult(
        string resourceType,
        string action,
        string reason,
        string? policy = null)
        => new()
        {
            Allowed = false,
            Reason = reason,
            Policy = policy,
            ResourceType = resourceType,
            Action = action
        };
}