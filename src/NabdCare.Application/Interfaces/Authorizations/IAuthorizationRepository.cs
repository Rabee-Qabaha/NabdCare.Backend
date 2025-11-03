namespace NabdCare.Application.Interfaces.Authorizations;

/// <summary>
/// Repository interface for authorization resource queries.
/// Follows clean architecture by abstracting data access from service layer.
/// </summary>
public interface IAuthorizationRepository
{
    /// <summary>
    /// Get user by ID with all related data needed for authorization
    /// </summary>
    Task<object?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Get clinic by ID
    /// </summary>
    Task<object?> GetClinicByIdAsync(Guid clinicId);

    /// <summary>
    /// Get role by ID
    /// </summary>
    Task<object?> GetRoleByIdAsync(Guid roleId);

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    Task<object?> GetSubscriptionByIdAsync(Guid subscriptionId);

    /// <summary>
    /// Get patient by ID
    /// </summary>
    // Task<object?> GetPatientByIdAsync(Guid patientId);

    /// <summary>
    /// Get payment by ID
    /// </summary>
    Task<object?> GetPaymentByIdAsync(Guid paymentId);

    /// <summary>
    /// Get medical record by ID
    /// </summary>
    // Task<object?> GetMedicalRecordByIdAsync(Guid recordId);

    /// <summary>
    /// Get appointment by ID
    /// </summary>
    // Task<object?> GetAppointmentByIdAsync(Guid appointmentId);
}