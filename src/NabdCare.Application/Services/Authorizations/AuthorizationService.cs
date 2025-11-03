using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Authorizations;
using NabdCare.Application.Interfaces.Authorizations;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Authorizations;

/// <summary>
/// Service for checking authorization on specific resources.
/// Implements two-tier authorization:
/// 1. RBAC/PBAC: User has required permission
/// 2. ABAC: Policy allows access to specific resource
/// 
/// Uses IAuthorizationRepository for data access (clean architecture).
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly ITenantContext _tenantContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthorizationService> _logger;

    // Cache configuration
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
        resourceType = resourceType?.ToLower().Trim() ?? string.Empty;
        resourceId = resourceId?.Trim() ?? string.Empty;
        action = action?.ToLower().Trim() ?? string.Empty;

        // Validate inputs
        if (string.IsNullOrEmpty(resourceType))
            return CreateDeniedResult(resourceType, action, "Invalid resource type");

        if (string.IsNullOrEmpty(resourceId) || !Guid.TryParse(resourceId, out var resourceGuid))
            return CreateDeniedResult(resourceType, action, "Invalid resource ID format");

        if (string.IsNullOrEmpty(action))
            return CreateDeniedResult(resourceType, action, "Invalid action");

        // Check cache
        var cacheKey = GetCacheKey(resourceGuid, resourceType, action);
        if (_cache.TryGetValue(cacheKey, out AuthorizationResultDto? cached))
        {
            _logger.LogDebug("✅ Cache hit for authorization check: {ResourceType}:{ResourceId}:{Action}",
                resourceType, resourceId, action);
            return cached!;
        }

        // Evaluate authorization
        var result = await EvaluateAuthorizationAsync(resourceType, resourceGuid, action);

        // Cache result
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
        try
        {
            // Normalize resource type
            var normalizedType = resourceType.ToLower();

            // Route to appropriate evaluator
            return normalizedType switch
            {
                "user" => await EvaluateUserAuthorizationAsync(resourceId, action),
                "clinic" => await EvaluateClinicAuthorizationAsync(resourceId, action),
                "role" => await EvaluateRoleAuthorizationAsync(resourceId, action),
                "subscription" => await EvaluateSubscriptionAuthorizationAsync(resourceId, action),
                // "patient" => await EvaluatePatientAuthorizationAsync(resourceId, action),
                "payment" => await EvaluatePaymentAuthorizationAsync(resourceId, action),
                // "medicalrecord" => await EvaluateMedicalRecordAuthorizationAsync(resourceId, action),
                // "appointment" => await EvaluateAppointmentAuthorizationAsync(resourceId, action),
                _ => CreateDeniedResult(resourceType, action, $"Unknown resource type: {resourceType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating authorization for {ResourceType}:{ResourceId}:{Action}",
                resourceType, resourceId, action);
            return CreateDeniedResult(resourceType, action, "Authorization check failed");
        }
    }

    // ============================================================
    // RESOURCE-SPECIFIC EVALUATORS
    // ============================================================

    private async Task<AuthorizationResultDto> EvaluateUserAuthorizationAsync(Guid userId, string action)
    {
        // Check RBAC/PBAC first
        var hasPermission = await _permissionEvaluator.HasAsync("Users.View");
        if (!hasPermission)
            return CreateDeniedResult("user", action, "Missing Users.View permission", policy: null);

        // Load user via repository
        var user = await _authorizationRepository.GetUserByIdAsync(userId);

        if (user == null)
            return CreateDeniedResult("user", action, "User not found", policy: null);

        // Check ABAC policy
        var allowed = await _permissionEvaluator.CanAsync("Users.View", action, user);
        if (!allowed)
        {
            _logger.LogWarning("🚫 Access denied by UserPolicy for user {UserId} action {Action} by user {CurrentUserId}",
                userId, action, _tenantContext.UserId);
            return CreateDeniedResult("user", action, "User belongs to a different clinic", policy: "UserPolicy");
        }

        _logger.LogInformation("✅ Authorization allowed: User {UserId} action {Action}", userId, action);
        return CreateAllowedResult("user", action);
    }

    private async Task<AuthorizationResultDto> EvaluateClinicAuthorizationAsync(Guid clinicId, string action)
    {
        var hasPermission = await _permissionEvaluator.HasAsync("Clinics.ViewAll");
        if (!hasPermission)
            return CreateDeniedResult("clinic", action, "Missing Clinics.ViewAll permission", policy: null);

        // Load clinic via repository
        var clinic = await _authorizationRepository.GetClinicByIdAsync(clinicId);

        if (clinic == null)
            return CreateDeniedResult("clinic", action, "Clinic not found", policy: null);

        var allowed = await _permissionEvaluator.CanAsync("Clinics.ViewAll", action, clinic);
        if (!allowed)
        {
            _logger.LogWarning("🚫 Access denied by ClinicPolicy for clinic {ClinicId} action {Action}", clinicId, action);
            return CreateDeniedResult("clinic", action, "Clinic is not accessible", policy: "ClinicPolicy");
        }

        _logger.LogInformation("✅ Authorization allowed: Clinic {ClinicId} action {Action}", clinicId, action);
        return CreateAllowedResult("clinic", action);
    }

    private async Task<AuthorizationResultDto> EvaluateRoleAuthorizationAsync(Guid roleId, string action)
    {
        var hasPermission = await _permissionEvaluator.HasAsync("Roles.View");
        if (!hasPermission)
            return CreateDeniedResult("role", action, "Missing Roles.View permission", policy: null);

        // Load role via repository
        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);

        if (role == null)
            return CreateDeniedResult("role", action, "Role not found", policy: null);

        var allowed = await _permissionEvaluator.CanAsync("Roles.View", action, role);
        if (!allowed)
        {
            _logger.LogWarning("🚫 Access denied by RolePolicy for role {RoleId} action {Action}", roleId, action);
            return CreateDeniedResult("role", action, "Role is not accessible", policy: "RolePolicy");
        }

        _logger.LogInformation("✅ Authorization allowed: Role {RoleId} action {Action}", roleId, action);
        return CreateAllowedResult("role", action);
    }

    private async Task<AuthorizationResultDto> EvaluateSubscriptionAuthorizationAsync(Guid subscriptionId, string action)
    {
        var hasPermission = await _permissionEvaluator.HasAsync("Subscriptions.View");
        if (!hasPermission)
            return CreateDeniedResult("subscription", action, "Missing Subscriptions.View permission", policy: null);

        // Load subscription via repository
        var subscription = await _authorizationRepository.GetSubscriptionByIdAsync(subscriptionId);

        if (subscription == null)
            return CreateDeniedResult("subscription", action, "Subscription not found", policy: null);

        var allowed = await _permissionEvaluator.CanAsync("Subscriptions.View", action, subscription);
        if (!allowed)
        {
            _logger.LogWarning("🚫 Access denied by SubscriptionPolicy for subscription {SubscriptionId} action {Action}",
                subscriptionId, action);
            return CreateDeniedResult("subscription", action, "Subscription belongs to a different clinic",
                policy: "SubscriptionPolicy");
        }

        _logger.LogInformation("✅ Authorization allowed: Subscription {SubscriptionId} action {Action}",
            subscriptionId, action);
        return CreateAllowedResult("subscription", action);
    }

    // private async Task<AuthorizationResultDto> EvaluatePatientAuthorizationAsync(Guid patientId, string action)
    // {
    //     var hasPermission = await _permissionEvaluator.HasAsync("Patients.View");
    //     if (!hasPermission)
    //         return CreateDeniedResult("patient", action, "Missing Patients.View permission", policy: null);
    //
    //     // Load patient via repository
    //     var patient = await _authorizationRepository.GetPatientByIdAsync(patientId);
    //
    //     if (patient == null)
    //         return CreateDeniedResult("patient", action, "Patient not found", policy: null);
    //
    //     var allowed = await _permissionEvaluator.CanAsync("Patients.View", action, patient);
    //     if (!allowed)
    //     {
    //         _logger.LogWarning("🚫 Access denied by PatientPolicy for patient {PatientId} action {Action}",
    //             patientId, action);
    //         return CreateDeniedResult("patient", action, "Patient belongs to a different clinic", policy: "PatientPolicy");
    //     }
    //
    //     _logger.LogInformation("✅ Authorization allowed: Patient {PatientId} action {Action}", patientId, action);
    //     return CreateAllowedResult("patient", action);
    // }

    private async Task<AuthorizationResultDto> EvaluatePaymentAuthorizationAsync(Guid paymentId, string action)
    {
        var hasPermission = await _permissionEvaluator.HasAsync("Payments.View");
        if (!hasPermission)
            return CreateDeniedResult("payment", action, "Missing Payments.View permission", policy: null);

        // Load payment via repository
        var payment = await _authorizationRepository.GetPaymentByIdAsync(paymentId);

        if (payment == null)
            return CreateDeniedResult("payment", action, "Payment not found", policy: null);

        var allowed = await _permissionEvaluator.CanAsync("Payments.View", action, payment);
        if (!allowed)
        {
            _logger.LogWarning("🚫 Access denied by PaymentPolicy for payment {PaymentId} action {Action}",
                paymentId, action);
            return CreateDeniedResult("payment", action, "Payment belongs to a different clinic", policy: "PaymentPolicy");
        }

        _logger.LogInformation("✅ Authorization allowed: Payment {PaymentId} action {Action}", paymentId, action);
        return CreateAllowedResult("payment", action);
    }

    // private async Task<AuthorizationResultDto> EvaluateMedicalRecordAuthorizationAsync(Guid recordId, string action)
    // {
    //     var hasPermission = await _permissionEvaluator.HasAsync("MedicalRecords.View");
    //     if (!hasPermission)
    //         return CreateDeniedResult("medicalrecord", action, "Missing MedicalRecords.View permission", policy: null);
    //
    //     // Load medical record via repository
    //     var record = await _authorizationRepository.GetMedicalRecordByIdAsync(recordId);
    //
    //     if (record == null)
    //         return CreateDeniedResult("medicalrecord", action, "Medical record not found", policy: null);
    //
    //     var allowed = await _permissionEvaluator.CanAsync("MedicalRecords.View", action, record);
    //     if (!allowed)
    //     {
    //         _logger.LogWarning("🚫 Access denied for medical record {RecordId} action {Action}", recordId, action);
    //         return CreateDeniedResult("medicalrecord", action, "Medical record is not accessible", policy: "DefaultPolicy");
    //     }
    //
    //     _logger.LogInformation("✅ Authorization allowed: Medical Record {RecordId} action {Action}", recordId, action);
    //     return CreateAllowedResult("medicalrecord", action);
    // }

    // private async Task<AuthorizationResultDto> EvaluateAppointmentAuthorizationAsync(Guid appointmentId, string action)
    // {
    //     var hasPermission = await _permissionEvaluator.HasAsync("Appointments.View");
    //     if (!hasPermission)
    //         return CreateDeniedResult("appointment", action, "Missing Appointments.View permission", policy: null);
    //
    //     // Load appointment via repository
    //     var appointment = await _authorizationRepository.GetAppointmentByIdAsync(appointmentId);
    //
    //     if (appointment == null)
    //         return CreateDeniedResult("appointment", action, "Appointment not found", policy: null);
    //
    //     var allowed = await _permissionEvaluator.CanAsync("Appointments.View", action, appointment);
    //     if (!allowed)
    //     {
    //         _logger.LogWarning("🚫 Access denied for appointment {AppointmentId} action {Action}", appointmentId, action);
    //         return CreateDeniedResult("appointment", action, "Appointment is not accessible", policy: "DefaultPolicy");
    //     }
    //
    //     _logger.LogInformation("✅ Authorization allowed: Appointment {AppointmentId} action {Action}",
    //         appointmentId, action);
    //     return CreateAllowedResult("appointment", action);
    // }

    // ============================================================
    // HELPER METHODS
    // ============================================================

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