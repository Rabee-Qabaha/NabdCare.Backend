using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;

namespace NabdCare.Application.Interfaces.Clinics.Subscriptions;

/// <summary>
/// Business service for handling subscription operations.
/// Applies business logic, validation, and access control.
/// </summary>
public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto);
    Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false);

    /// <summary>
    /// Get paginated subscriptions by clinic
    /// </summary>
    Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto pagination, bool includePayments = false);

    /// <summary>
    /// Get all subscriptions (SuperAdmin only, paginated)
    /// </summary>
    Task<PaginatedResult<SubscriptionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination, bool includePayments = false);

    /// <summary>
    /// Get paged subscriptions (with filter or sorting)
    /// </summary>
    Task<PaginatedResult<SubscriptionResponseDto>> GetPagedAsync(PaginationRequestDto pagination, bool includePayments = false);

    Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto);
    Task<bool> SoftDeleteSubscriptionAsync(Guid id);
    Task<bool> DeleteSubscriptionAsync(Guid id);
}