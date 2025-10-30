using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Pagination;
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
    /// Get all clinics (SuperAdmin only) - paginated
    /// </summary>
    Task<PaginatedResult<ClinicResponseDto>> GetAllClinicsPagedAsync(PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics by subscription status (SuperAdmin only) - paginated
    /// </summary>
    Task<PaginatedResult<ClinicResponseDto>> GetClinicsByStatusPagedAsync(SubscriptionStatus status, PaginationRequestDto pagination);

    /// <summary>
    /// Get active clinics with valid subscriptions (SuperAdmin only) - paginated
    /// </summary>
    Task<PaginatedResult<ClinicResponseDto>> GetActiveClinicsPagedAsync(PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics with expiring subscriptions (SuperAdmin only) - paginated
    /// </summary>
    Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsPagedAsync(int withinDays, PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics with expired subscriptions (SuperAdmin only) - paginated
    /// </summary>
    Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination);

    /// <summary>
    /// Search clinics by name, email, or phone (SuperAdmin only) - paginated
    /// </summary>
    Task<PaginatedResult<ClinicResponseDto>> SearchClinicsPagedAsync(string query, PaginationRequestDto pagination);

    // ============================================
    // COMMAND METHODS
    // ============================================

    Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto);
    Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto);
    Task<ClinicResponseDto?> UpdateClinicStatusAsync(Guid id, UpdateClinicStatusDto dto);
    Task<ClinicResponseDto?> ActivateClinicAsync(Guid id);
    Task<ClinicResponseDto?> SuspendClinicAsync(Guid id);
    Task<bool> SoftDeleteClinicAsync(Guid id);
    Task<bool> DeleteClinicAsync(Guid id);

    // ============================================
    // STATISTICS
    // ============================================

    Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id);
}