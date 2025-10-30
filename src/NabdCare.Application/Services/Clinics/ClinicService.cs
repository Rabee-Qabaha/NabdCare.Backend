using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Clinics;

/// <summary>
/// Production-ready clinic service following clean architecture.
/// No try-catch blocks - exceptions bubble up to middleware.
/// </summary>
public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<ClinicService> logger)
    {
        _clinicRepository = clinicRepository ?? throw new ArgumentNullException(nameof(clinicRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

    public async Task<ClinicResponseDto?> GetClinicByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        var clinic = await _clinicRepository.GetByIdAsync(id);

        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found", id);
            return null;
        }

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
            throw new UnauthorizedAccessException("You can only view your own clinic");

        return _mapper.Map<ClinicResponseDto>(clinic);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetAllClinicsPagedAsync(PaginationRequestDto pagination)
    {
        EnsureSuperAdmin();

        var result = await _clinicRepository.GetAllPagedAsync(pagination);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsByStatusPagedAsync(SubscriptionStatus status, PaginationRequestDto pagination)
    {
        EnsureSuperAdmin();

        var result = await _clinicRepository.GetByStatusPagedAsync(status, pagination);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetActiveClinicsPagedAsync(PaginationRequestDto pagination)
    {
        EnsureSuperAdmin();

        var result = await _clinicRepository.GetActiveWithValidSubscriptionPagedAsync(pagination);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsPagedAsync(int withinDays, PaginationRequestDto pagination)
    {
        EnsureSuperAdmin();

        if (withinDays < 1 || withinDays > 365)
            throw new ArgumentException("Days must be between 1 and 365", nameof(withinDays));

        var result = await _clinicRepository.GetWithExpiringSubscriptionsPagedAsync(withinDays, pagination);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination)
    {
        EnsureSuperAdmin();

        var result = await _clinicRepository.GetWithExpiredSubscriptionsPagedAsync(pagination);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> SearchClinicsPagedAsync(string query, PaginationRequestDto pagination)
    {
        EnsureSuperAdmin();

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty", nameof(query));

        var result = await _clinicRepository.SearchPagedAsync(query, pagination);
        return MapPaginated(result);
    }

    #endregion

    #region COMMAND METHODS

    public async Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        ValidateCreateClinicDto(dto);

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} creating clinic {ClinicName}", currentUserId, dto.Name);

        // Check uniqueness
        if (await _clinicRepository.ExistsByNameAsync(dto.Name))
        {
            _logger.LogWarning("Attempted to create clinic with duplicate name: {Name}", dto.Name);
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email))
        {
            _logger.LogWarning("Attempted to create clinic with duplicate email: {Email}", dto.Email);
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists");
        }

        // Create clinic entity
        var clinic = _mapper.Map<Clinic>(dto);
        clinic.Id = Guid.NewGuid();
        clinic.Status = dto.Status;
        clinic.CreatedAt = DateTime.UtcNow;
        clinic.CreatedBy = currentUserId;
        clinic.IsDeleted = false;

        // Create initial subscription
        var subscriptionId = Guid.NewGuid();
        clinic.Subscriptions = new List<Subscription>
        {
            new Subscription
            {
                Id = subscriptionId,
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

        _logger.LogInformation("User {CurrentUserId} successfully created clinic {ClinicId} ({ClinicName}) with subscription {SubscriptionId}",
            currentUserId, created.Id, created.Name, subscriptionId);

        return _mapper.Map<ClinicResponseDto>(created);
    }

    public async Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        ValidateUpdateClinicDto(dto);

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} updating clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for update", id);
            return null;
        }

        // Multi-tenant security: SuperAdmin can update all, others only their own
        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to update clinic {ClinicId} without permission", currentUserId, id);
            throw new UnauthorizedAccessException("You can only update your own clinic");
        }

        // Check uniqueness (excluding current clinic)
        if (await _clinicRepository.ExistsByNameAsync(dto.Name, id))
        {
            _logger.LogWarning("Attempted to update clinic with duplicate name: {Name}", dto.Name);
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email, id))
        {
            _logger.LogWarning("Attempted to update clinic with duplicate email: {Email}", dto.Email);
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists");
        }

        // Update clinic fields
        clinic.Name = dto.Name.Trim();
        clinic.Email = dto.Email?.Trim();
        clinic.Phone = dto.Phone?.Trim();
        clinic.Address = dto.Address?.Trim();
        clinic.BranchCount = dto.BranchCount;
        clinic.Status = dto.Status;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        // Update latest subscription
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (latestSubscription != null)
        {
            latestSubscription.StartDate = dto.SubscriptionStartDate;
            latestSubscription.EndDate = dto.SubscriptionEndDate;
            latestSubscription.Fee = dto.SubscriptionFee;
            latestSubscription.Type = dto.SubscriptionType;
            latestSubscription.Status = dto.Status;
            latestSubscription.UpdatedAt = DateTime.UtcNow;
            latestSubscription.UpdatedBy = currentUserId;
        }
        else
        {
            // Create subscription if none exists
            clinic.Subscriptions ??= new List<Subscription>();
            clinic.Subscriptions.Add(new Subscription
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
            });
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);

        _logger.LogInformation("User {CurrentUserId} successfully updated clinic {ClinicId} ({ClinicName})",
            currentUserId, id, updated.Name);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> UpdateClinicStatusAsync(Guid id, UpdateClinicStatusDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to update clinic status", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can update clinic status");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} updating status for clinic {ClinicId} to {Status}",
            currentUserId, id, dto.Status);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for status update", id);
            return null;
        }

        clinic.Status = dto.Status;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        // Also update latest subscription status
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (latestSubscription != null)
        {
            latestSubscription.Status = dto.Status;
            latestSubscription.UpdatedAt = DateTime.UtcNow;
            latestSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);

        _logger.LogInformation("SuperAdmin {CurrentUserId} successfully updated status for clinic {ClinicId} to {Status}",
            currentUserId, id, dto.Status);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> ActivateClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to activate clinic {ClinicId}", currentUserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can activate clinics");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} activating clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for activation", id);
            return null;
        }

        if (clinic.Status == SubscriptionStatus.Active)
        {
            _logger.LogInformation("Clinic {ClinicId} is already active", id);
            return _mapper.Map<ClinicResponseDto>(clinic);
        }

        clinic.Status = SubscriptionStatus.Active;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        // Activate latest subscription
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (latestSubscription != null)
        {
            latestSubscription.Status = SubscriptionStatus.Active;
            latestSubscription.UpdatedAt = DateTime.UtcNow;
            latestSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);

        _logger.LogInformation("SuperAdmin {CurrentUserId} successfully activated clinic {ClinicId} ({ClinicName})",
            currentUserId, id, updated.Name);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> SuspendClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to suspend clinic {ClinicId}", currentUserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can suspend clinics");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} suspending clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for suspension", id);
            return null;
        }

        if (clinic.Status == SubscriptionStatus.Suspended)
        {
            _logger.LogInformation("Clinic {ClinicId} is already suspended", id);
            return _mapper.Map<ClinicResponseDto>(clinic);
        }

        clinic.Status = SubscriptionStatus.Suspended;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

        // Suspend latest subscription
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (latestSubscription != null)
        {
            latestSubscription.Status = SubscriptionStatus.Suspended;
            latestSubscription.UpdatedAt = DateTime.UtcNow;
            latestSubscription.UpdatedBy = currentUserId;
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);

        _logger.LogWarning("SuperAdmin {CurrentUserId} suspended clinic {ClinicId} ({ClinicName})",
            currentUserId, id, updated.Name);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to delete clinic {ClinicId}", currentUserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can delete clinics");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} soft deleting clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for soft delete", id);
            return false;
        }

        var success = await _clinicRepository.SoftDeleteAsync(id);

        if (success)
        {
            _logger.LogInformation("SuperAdmin {CurrentUserId} successfully soft deleted clinic {ClinicId} ({ClinicName})",
                currentUserId, id, clinic.Name);
        }

        return success;
    }

    public async Task<bool> DeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to permanently delete clinic {ClinicId}", currentUserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete clinics");
        }

        _logger.LogWarning("SuperAdmin {CurrentUserId} permanently deleting clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for permanent delete", id);
            return false;
        }

        var success = await _clinicRepository.DeleteAsync(id);

        if (success)
        {
            _logger.LogWarning("⚠️ SuperAdmin {CurrentUserId} PERMANENTLY DELETED clinic {ClinicId} ({ClinicName})",
                currentUserId, id, clinic.Name);
        }

        return success;
    }

    #endregion

    #region STATISTICS

    public async Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view clinic statistics", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view clinic statistics");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving statistics for clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for statistics", id);
            return null;
        }

        var now = DateTime.UtcNow;
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted)
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
            IsExpiringSoon = daysUntilExpiration > 0 && daysUntilExpiration <= 30,
            CreatedAt = clinic.CreatedAt
        };

        return statistics;
    }

    #endregion

    #region HELPER METHODS

    private void ValidateCreateClinicDto(CreateClinicRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Clinic name is required", nameof(dto.Name));

        if (dto.SubscriptionStartDate >= dto.SubscriptionEndDate)
            throw new ArgumentException("Subscription start date must be before end date", nameof(dto.SubscriptionStartDate));

        if (dto.SubscriptionFee < 0)
            throw new ArgumentException("Subscription fee cannot be negative", nameof(dto.SubscriptionFee));

        if (dto.BranchCount < 1)
            throw new ArgumentException("Branch count must be at least 1", nameof(dto.BranchCount));
    }

    private void ValidateUpdateClinicDto(UpdateClinicRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Clinic name is required", nameof(dto.Name));

        if (dto.SubscriptionStartDate >= dto.SubscriptionEndDate)
            throw new ArgumentException("Subscription start date must be before end date", nameof(dto.SubscriptionStartDate));

        if (dto.SubscriptionFee < 0)
            throw new ArgumentException("Subscription fee cannot be negative", nameof(dto.SubscriptionFee));

        if (dto.BranchCount < 1)
            throw new ArgumentException("Branch count must be at least 1", nameof(dto.BranchCount));
    }
    private void EnsureSuperAdmin()
    {
        var userId = _userContext.GetCurrentUserId();
        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {UserId} attempted a restricted action", userId);
            throw new UnauthorizedAccessException("Only SuperAdmin can perform this action");
        }
    }

    private PaginatedResult<ClinicResponseDto> MapPaginated(PaginatedResult<Clinic> result)
    {
        return new PaginatedResult<ClinicResponseDto>
        {
            Items = _mapper.Map<IEnumerable<ClinicResponseDto>>(result.Items),
            HasMore = result.HasMore,
            NextCursor = result.NextCursor,
            TotalCount = result.TotalCount
        };
    }
    #endregion
}