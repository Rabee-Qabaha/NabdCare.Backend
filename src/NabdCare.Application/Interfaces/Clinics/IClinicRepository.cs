using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Clinics
{
    /// <summary>
    /// Repository interface for clinic data access operations.
    /// Thin data access layer - no business logic.
    /// </summary>
    public interface IClinicRepository
    {
        // ============================================
        // QUERY METHODS
        // ============================================

        Task<Clinic?> GetByIdAsync(Guid id);
        
        Task<Clinic?> GetEntityByIdAsync(Guid id);

        /// <summary>
        /// Get all clinics (SuperAdmin only) with pagination, sorting, and filtering
        /// </summary>
        Task<PaginatedResult<Clinic>> GetAllPagedAsync(
            ClinicFilterRequestDto filters,
            Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null);

        Task<PaginatedResult<Clinic>> GetByStatusPagedAsync(
            SubscriptionStatus status,
            PaginationRequestDto pagination,
            Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null);

        Task<PaginatedResult<Clinic>> GetActiveWithValidSubscriptionPagedAsync(
            PaginationRequestDto pagination,
            Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null);

        Task<PaginatedResult<Clinic>> GetWithExpiringSubscriptionsPagedAsync(
            int withinDays,
            PaginationRequestDto pagination,
            Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null);

        Task<PaginatedResult<Clinic>> GetWithExpiredSubscriptionsPagedAsync(
            PaginationRequestDto pagination,
            Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null);

        Task<IEnumerable<Clinic>> GetWithExpiredSubscriptionsAsync();

        Task<PaginatedResult<Clinic>> SearchPagedAsync(
            string query,
            PaginationRequestDto pagination,
            Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null);

        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
        Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null);
        
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null);
        
        Task<bool> ExistsAsync(Guid id);

        // ============================================
        // COMMAND METHODS
        // ============================================

        Task<Clinic> CreateAsync(Clinic clinic);
        Task<Clinic> UpdateAsync(Clinic clinic);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);

        // ============================================
        // STATISTICS
        // ============================================

        Task<int> GetTotalCountAsync();
        Task<int> GetCountByStatusAsync(SubscriptionStatus status);
        Task<int> GetActiveCountAsync();
    }
}