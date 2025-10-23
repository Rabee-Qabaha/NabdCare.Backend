using NabdCare.Application.DTOs.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Clinics;

/// <summary>
/// Service interface for clinic business operations.
/// Handles business logic, validation, and multi-tenant security.
/// </summary>
public interface IClinicService
{
    // ============================================
    // QUERY METHODS
    // ============================================
    
    /// <summary>
    /// Get clinic by ID (tenant-filtered for non-SuperAdmin)
    /// </summary>
    Task<ClinicResponseDto?> GetClinicByIdAsync(Guid id);
    
    /// <summary>
    /// Get all clinics (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> GetAllClinicsAsync();
    
    /// <summary>
    /// Get clinics by subscription status (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> GetClinicsByStatusAsync(SubscriptionStatus status);
    
    /// <summary>
    /// Get active clinics with valid subscriptions (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> GetActiveClinicsAsync();
    
    /// <summary>
    /// Get clinics with expiring subscriptions (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsAsync(int withinDays);
    
    /// <summary>
    /// Get clinics with expired subscriptions (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsAsync();
    
    /// <summary>
    /// Get paginated clinics (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> GetPagedClinicsAsync(int page, int pageSize);
    
    /// <summary>
    /// Search clinics by name, email, or phone (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<ClinicResponseDto>> SearchClinicsAsync(string query);

    // ============================================
    // COMMAND METHODS
    // ============================================
    
    /// <summary>
    /// Create new clinic with initial subscription (SuperAdmin only)
    /// </summary>
    Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto);
    
    /// <summary>
    /// Update clinic (SuperAdmin or own clinic only)
    /// </summary>
    Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto);
    
    /// <summary>
    /// Update clinic subscription status (SuperAdmin only)
    /// </summary>
    Task<ClinicResponseDto?> UpdateClinicStatusAsync(Guid id, UpdateClinicStatusDto dto);
    
    /// <summary>
    /// Activate clinic (SuperAdmin only)
    /// </summary>
    Task<ClinicResponseDto?> ActivateClinicAsync(Guid id);
    
    /// <summary>
    /// Suspend clinic (SuperAdmin only)
    /// </summary>
    Task<ClinicResponseDto?> SuspendClinicAsync(Guid id);
    
    /// <summary>
    /// Soft delete clinic (SuperAdmin only)
    /// </summary>
    Task<bool> SoftDeleteClinicAsync(Guid id);
    
    /// <summary>
    /// Permanently delete clinic (SuperAdmin only)
    /// </summary>
    Task<bool> DeleteClinicAsync(Guid id);

    // ============================================
    // STATISTICS
    // ============================================
    
    /// <summary>
    /// Get clinic statistics (SuperAdmin only)
    /// </summary>
    Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id);
}