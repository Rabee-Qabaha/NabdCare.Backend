using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Authorizations;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Authorization;

/// <summary>
/// Repository for authorization resource queries.
/// Implements IAuthorizationRepository to provide data access for authorization checks.
/// </summary>
public class AuthorizationRepository : IAuthorizationRepository
{
    private readonly NabdCareDbContext _dbContext;

    public AuthorizationRepository(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Get user by ID for authorization checks
    /// Uses IgnoreQueryFilters to access all users regardless of soft delete or tenant filters
    /// </summary>
    public async Task<object?> GetUserByIdAsync(Guid userId)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Get clinic by ID for authorization checks
    /// </summary>
    public async Task<object?> GetClinicByIdAsync(Guid clinicId)
    {
        return await _dbContext.Clinics
            .FirstOrDefaultAsync(c => c.Id == clinicId);
    }

    /// <summary>
    /// Get role by ID for authorization checks
    /// Uses IgnoreQueryFilters to access system and template roles
    /// </summary>
    public async Task<object?> GetRoleByIdAsync(Guid roleId)
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == roleId);
    }

    /// <summary>
    /// Get subscription by ID for authorization checks
    /// </summary>
    public async Task<object?> GetSubscriptionByIdAsync(Guid subscriptionId)
    {
        return await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    /// <summary>
    /// Get patient by ID for authorization checks
    /// </summary>
    
    // TODO: uncomment when implemented
    // public async Task<object?> GetPatientByIdAsync(Guid patientId)
    // {
    //     return await _dbContext.Patients
    //         .FirstOrDefaultAsync(p => p.Id == patientId);
    // }

    /// <summary>
    /// Get payment by ID for authorization checks
    /// </summary>
    public async Task<object?> GetPaymentByIdAsync(Guid paymentId)
    {
        return await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    /// <summary>
    /// Get medical record by ID for authorization checks
    /// </summary>
    // TODO: uncomment when implemented
    // public async Task<object?> GetMedicalRecordByIdAsync(Guid recordId)
    // {
    //     // Assuming MedicalRecord entity exists or adjust based on your actual entity name
    //     return await _dbContext.MedicalRecords
    //         .FirstOrDefaultAsync(m => m.Id == recordId);
    // }

    /// <summary>
    /// Get appointment by ID for authorization checks
    /// </summary>
    
    // TODO: uncomment when implemented
    // public async Task<object?> GetAppointmentByIdAsync(Guid appointmentId)
    // {
    //     // Assuming Appointment entity exists or adjust based on your actual entity name
    //     return await _dbContext.Appointments
    //         .FirstOrDefaultAsync(a => a.Id == appointmentId);
    // }
}