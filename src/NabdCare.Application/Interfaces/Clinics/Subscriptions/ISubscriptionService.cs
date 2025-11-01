// NabdCare.Application/Interfaces/Clinics/Subscriptions/ISubscriptionService.cs
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Clinics.Subscriptions;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto);
    Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false);

    Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null);

    Task<PaginatedResult<SubscriptionResponseDto>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null);

    Task<PaginatedResult<SubscriptionResponseDto>> GetPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null);

    Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto);
    Task<bool> SoftDeleteSubscriptionAsync(Guid id);
    Task<bool> DeleteSubscriptionAsync(Guid id);

    Task<SubscriptionResponseDto?> UpdateSubscriptionStatusAsync(Guid id, SubscriptionStatus newStatus);

    Task<SubscriptionResponseDto?> ToggleAutoRenewAsync(Guid id, bool enable);
    
    /// <summary>
    /// Run auto-renew for all candidates (background job).
    /// Returns number of new subscriptions created.
    /// </summary>
    Task<int> ProcessAutoRenewalsAsync(DateTime nowUtc);
    
    /// <summary>
    /// Manual single renewal (admin action).
    /// </summary>
    Task<SubscriptionResponseDto> RenewSubscriptionAsync(Guid oldSubscriptionId, SubscriptionType type);
}