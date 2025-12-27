using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Clinics;

public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ClinicService> _logger;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public ClinicService(
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<ClinicService> logger,
        IPermissionEvaluator permissionEvaluator)
    {
        _clinicRepository = clinicRepository ?? throw new ArgumentNullException(nameof(clinicRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    // ============================================
    // QUERY METHODS
    // ============================================

    public async Task<ClinicResponseDto?> GetClinicByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogDebug("User {UserId} retrieving clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);

        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found. Error code: {ErrorCode}", id, ErrorCodes.NOT_FOUND);
            return null;
        }

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
        {
            _logger.LogWarning("User {UserId} attempted to view clinic {ClinicId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"You can only view your own clinic. Error code: {ErrorCodes.FORBIDDEN}");
        }

        return _mapper.Map<ClinicResponseDto>(clinic);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetAllClinicsPagedAsync(ClinicFilterRequestDto filters)
    {
        if (filters == null) throw new ArgumentNullException(nameof(filters));

        var currentUserId = _userContext.GetCurrentUserId();
    
        // Security Check: Only SuperAdmin should see deleted clinics
        // We modify the DTO property directly if the user is unauthorized
        if (filters.IncludeDeleted && !_tenantContext.IsSuperAdmin)
        {
            filters.IncludeDeleted = false;
        }

        _logger.LogInformation("User {UserId} retrieving clinics (Limit={Limit}, IncludeDeleted={Inc})", 
            currentUserId, filters.Limit, filters.IncludeDeleted);

        // Apply ABAC (Attribute-Based Access Control) filters
        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        // Pass the single DTO object to the repository
        var result = await _clinicRepository.GetAllPagedAsync(filters, abacFilter);

        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsByStatusPagedAsync(
        SubscriptionStatus status, PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetByStatusPagedAsync(status, pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetActiveClinicsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetActiveWithValidSubscriptionPagedAsync(pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsPagedAsync(
        int withinDays, PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));
        if (withinDays < 1 || withinDays > 365)
            throw new ArgumentException("Days must be between 1 and 365.", nameof(withinDays));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetWithExpiringSubscriptionsPagedAsync(withinDays, pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetWithExpiredSubscriptionsPagedAsync(pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> SearchClinicsPagedAsync(string query, PaginationRequestDto pagination)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty.", nameof(query));
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(q =>
            _permissionEvaluator.FilterClinics(q, "Clinics.View", _userContext));

        var result = await _clinicRepository.SearchPagedAsync(query, pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    // ============================================
    // COMMAND METHODS
    // ============================================

    public async Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} creating clinic {ClinicName}", currentUserId, dto.Name);

        // ✅ Business Rule 1: Check Name/Email Duplicates
        if (await _clinicRepository.ExistsByNameAsync(dto.Name))
        {
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email))
        {
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }
        
        // ✅ Business Rule 2: Check Slug (Subdomain) Uniqueness
        if (await _clinicRepository.ExistsBySlugAsync(dto.Slug))
        {
            throw new InvalidOperationException($"The subdomain '{dto.Slug}' is already taken. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

        var clinic = _mapper.Map<Clinic>(dto);
        clinic.Id = Guid.NewGuid();
        clinic.Status = dto.Status;
        clinic.CreatedAt = DateTime.UtcNow;
        clinic.CreatedBy = currentUserId;
        clinic.IsDeleted = false;

        // Create initial subscription
        clinic.Subscriptions = new List<Subscription>
        {
            new Subscription
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                StartDate = dto.SubscriptionStartDate,
                EndDate = dto.SubscriptionEndDate,
                Fee = dto.SubscriptionFee,
                Type = dto.SubscriptionType,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId,
                IsDeleted = false
            }
        };

        var created = await _clinicRepository.CreateAsync(clinic);

        _logger.LogInformation("User {CurrentUserId} created clinic {ClinicId}", currentUserId, created.Id);
        return _mapper.Map<ClinicResponseDto>(created);
    }

    public async Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} updating clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
            throw new UnauthorizedAccessException("You can only update your own clinic.");

        // ✅ Business Rule: Check Duplicates (excluding self)
        if (await _clinicRepository.ExistsByNameAsync(dto.Name, id))
            throw new InvalidOperationException($"Clinic name '{dto.Name}' already exists.");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email, id))
            throw new InvalidOperationException($"Clinic email '{dto.Email}' already exists.");
            
        if (await _clinicRepository.ExistsBySlugAsync(dto.Slug, id))
            throw new InvalidOperationException($"Subdomain '{dto.Slug}' is already taken.");

        // =========================================================
        // UPDATE FIELDS
        // =========================================================
        clinic.Name = dto.Name.Trim();
        clinic.Slug = dto.Slug.Trim(); // Update Subdomain
        clinic.Email = dto.Email.Trim();
        clinic.Phone = dto.Phone?.Trim();
        clinic.Address = dto.Address?.Trim();
        clinic.BranchCount = dto.BranchCount;
        clinic.Status = dto.Status;
        
        // Branding & Legal
        clinic.LogoUrl = dto.LogoUrl?.Trim();
        clinic.Website = dto.Website?.Trim();
        clinic.TaxNumber = dto.TaxNumber?.Trim();
        clinic.RegistrationNumber = dto.RegistrationNumber?.Trim();

        // Settings (JSONB)
        if (dto.Settings != null)
        {
            clinic.Settings = _mapper.Map<ClinicSettings>(dto.Settings);
        }

        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        // =========================================================
        // UPDATE SUBSCRIPTION (Safe Logic)
        // =========================================================
        var targetSubscription = GetCurrentOrLatestSubscription(clinic);

        if (targetSubscription != null)
        {
            targetSubscription.StartDate = dto.SubscriptionStartDate;
            targetSubscription.EndDate = dto.SubscriptionEndDate;
            targetSubscription.Fee = dto.SubscriptionFee;
            targetSubscription.Type = dto.SubscriptionType;
            targetSubscription.Status = dto.Status;
            targetSubscription.UpdatedAt = DateTime.UtcNow;
            targetSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> UpdateClinicStatusAsync(Guid id, UpdateClinicStatusDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can update clinic status.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        clinic.Status = dto.Status;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        // Update active subscription too
        var targetSubscription = GetCurrentOrLatestSubscription(clinic);
        if (targetSubscription != null)
        {
            targetSubscription.Status = dto.Status;
            targetSubscription.UpdatedAt = DateTime.UtcNow;
            targetSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> ActivateClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        
        var currentUserId = _userContext.GetCurrentUserId();
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can activate clinics.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        clinic.Status = SubscriptionStatus.Active;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        var targetSubscription = GetCurrentOrLatestSubscription(clinic);
        if (targetSubscription != null)
        {
            targetSubscription.Status = SubscriptionStatus.Active;
            targetSubscription.UpdatedAt = DateTime.UtcNow;
            targetSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);
        _logger.LogInformation("SuperAdmin {CurrentUserId} activated clinic {ClinicId}", currentUserId, id);
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> SuspendClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can suspend clinics.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        clinic.Status = SubscriptionStatus.Suspended;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        var targetSubscription = GetCurrentOrLatestSubscription(clinic);
        if (targetSubscription != null)
        {
            targetSubscription.Status = SubscriptionStatus.Suspended;
            targetSubscription.UpdatedAt = DateTime.UtcNow;
            targetSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);
        _logger.LogInformation("SuperAdmin {CurrentUserId} suspended clinic {ClinicId}", currentUserId, id);
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<bool> RestoreClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("User {UserId} attempted to restore clinic without permission.", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can restore deleted clinics.");
        }

        var success = await _clinicRepository.RestoreAsync(id);

        if (success)
            _logger.LogInformation("SuperAdmin {UserId} restored clinic {ClinicId}", currentUserId, id);
        else
            _logger.LogWarning("Failed to restore clinic {ClinicId} (Not found or not deleted)", id);

        return success;
    }
    
    public async Task<bool> SoftDeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can delete clinics.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return false;

        var result = await _clinicRepository.SoftDeleteAsync(id);
        if (result)
            _logger.LogInformation("SuperAdmin {CurrentUserId} soft deleted clinic {ClinicId}", currentUserId, id);

        return result;
    }

    public async Task<bool> DeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete clinics.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return false;

        var result = await _clinicRepository.DeleteAsync(id);
        if (result)
            _logger.LogWarning("SuperAdmin {CurrentUserId} permanently deleted clinic {ClinicId}", currentUserId, id);

        return result;
    }

    // ============================================
    // STATISTICS
    // ============================================

    public async Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can view clinic statistics.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        var now = DateTime.UtcNow;
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted && s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        var daysUntilExpiration = latestSubscription != null
            ? (int)(latestSubscription.EndDate - now).TotalDays
            : 0;

        var statistics = new ClinicStatisticsDto
        {
            ClinicId = clinic.Id,
            ClinicName = clinic.Name,
            Status = clinic.Status,
            BranchCount = clinic.BranchCount,
            TotalSubscriptions = clinic.Subscriptions?.Count(s => !s.IsDeleted) ?? 0,
            CurrentSubscription = latestSubscription != null
                ? _mapper.Map<SubscriptionResponseDto>(latestSubscription)
                : null,
            IsSubscriptionActive = latestSubscription != null
                                   && latestSubscription.EndDate > now
                                   && latestSubscription.Status == SubscriptionStatus.Active,
            DaysUntilExpiration = daysUntilExpiration,
            IsExpiringSoon = daysUntilExpiration is > 0 and <= 30,
            CreatedAt = clinic.CreatedAt
        };

        return statistics;
    }

    // ============================================
    // PRIVATE HELPERS
    // ============================================

    /// <summary>
    /// Smartly resolves the subscription to modify.
    /// Prioritizes the currently active one (covering NOW).
    /// If none, falls back to the latest future one.
    /// </summary>
    private Subscription? GetCurrentOrLatestSubscription(Clinic clinic)
    {
        if (clinic.Subscriptions == null || !clinic.Subscriptions.Any())
            return null;

        var now = DateTime.UtcNow;
        var activeSubscriptions = clinic.Subscriptions.Where(s => !s.IsDeleted).ToList();

        // 1. Try to find the one actually running right now
        var current = activeSubscriptions
            .FirstOrDefault(s => s.StartDate <= now && s.EndDate >= now);

        if (current != null) return current;

        // 2. If no current one, maybe we are setting up a future one? Get the latest.
        return activeSubscriptions.OrderByDescending(s => s.StartDate).FirstOrDefault();
    }
}