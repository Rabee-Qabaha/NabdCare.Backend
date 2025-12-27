using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Subscriptions;

public interface ISubscriptionService
{
    // CRUD
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto);
    Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false);
    Task<SubscriptionResponseDto?> GetActiveSubscriptionAsync(Guid clinicId);
    Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto);
    
    // Actions
    Task<bool> CancelSubscriptionAsync(Guid id);
    Task<SubscriptionResponseDto> RenewSubscriptionAsync(Guid oldSubscriptionId, SubscriptionType type);
    Task<SubscriptionResponseDto?> UpdateSubscriptionStatusAsync(Guid id, SubscriptionStatus newStatus);
    Task<SubscriptionResponseDto?> ToggleAutoRenewAsync(Guid id, bool enable);
    Task<bool> DeleteSubscriptionAsync(Guid id);

    // Automation Jobs
    Task<int> ProcessAutoRenewalsAsync(DateTime nowUtc);
    Task<int> ProcessScheduledCancellationsAsync(DateTime nowUtc);
    Task<int> ProcessExpirationsAsync(DateTime nowUtc);
    Task<int> ActivateFutureSubscriptionsAsync(DateTime nowUtc);

    // Queries (Cursor Paged)
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
}