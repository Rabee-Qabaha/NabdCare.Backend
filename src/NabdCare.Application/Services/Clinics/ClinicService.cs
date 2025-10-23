using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
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
        _logger.LogInformation("User {CurrentUserId} retrieving clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found", id);
            return null;
        }

        // Multi-tenant security: SuperAdmin can view all, others only their own
        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
        {
            _logger.LogWarning("User {CurrentUserId} (ClinicId: {UserClinicId}) attempted to access clinic {ClinicId} without permission",
                currentUserId, _tenantContext.ClinicId, id);
            throw new UnauthorizedAccessException("You can only view your own clinic");
        }

        return _mapper.Map<ClinicResponseDto>(clinic);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetAllClinicsAsync()
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view all clinics", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view all clinics");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving all clinics", currentUserId);

        var clinics = await _clinicRepository.GetAllAsync();
        var clinicsList = clinics.ToList();

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieved {Count} clinics", currentUserId, clinicsList.Count);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetClinicsByStatusAsync(SubscriptionStatus status)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view clinics by status", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view clinics by status");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving clinics with status {Status}", currentUserId, status);

        var clinics = await _clinicRepository.GetByStatusAsync(status);
        var clinicsList = clinics.ToList();

        _logger.LogInformation("Retrieved {Count} clinics with status {Status}", clinicsList.Count, status);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetActiveClinicsAsync()
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view active clinics", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view all clinics");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving active clinics with valid subscriptions", currentUserId);

        var clinics = await _clinicRepository.GetActiveWithValidSubscriptionAsync();
        var clinicsList = clinics.ToList();

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieved {Count} active clinics", currentUserId, clinicsList.Count);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsAsync(int withinDays)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view expiring subscriptions", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view expiring subscriptions");
        }

        if (withinDays < 1 || withinDays > 365)
            throw new ArgumentException("Days must be between 1 and 365", nameof(withinDays));

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving clinics with subscriptions expiring within {Days} days", 
            currentUserId, withinDays);

        var clinics = await _clinicRepository.GetWithExpiringSubscriptionsAsync(withinDays);
        var clinicsList = clinics.ToList();

        _logger.LogInformation("Retrieved {Count} clinics with expiring subscriptions", clinicsList.Count);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsAsync()
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view expired subscriptions", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view expired subscriptions");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving clinics with expired subscriptions", currentUserId);

        var clinics = await _clinicRepository.GetWithExpiredSubscriptionsAsync();
        var clinicsList = clinics.ToList();

        _logger.LogInformation("Retrieved {Count} clinics with expired subscriptions", clinicsList.Count);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetPagedClinicsAsync(int page, int pageSize)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view paged clinics", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view all clinics");
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving clinics page {Page} (size {PageSize})",
            currentUserId, page, pageSize);

        var clinics = await _clinicRepository.GetPagedAsync(page, pageSize);
        var clinicsList = clinics.ToList();

        _logger.LogInformation("Retrieved {Count} clinics for page {Page}", clinicsList.Count, page);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
    }

    public async Task<IEnumerable<ClinicResponseDto>> SearchClinicsAsync(string query)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to search clinics", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can search clinics");
        }

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty", nameof(query));

        _logger.LogInformation("SuperAdmin {CurrentUserId} searching clinics with query: {Query}", currentUserId, query);

        var clinics = await _clinicRepository.SearchAsync(query);
        var clinicsList = clinics.ToList();

        _logger.LogInformation("Search returned {Count} clinics", clinicsList.Count);

        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinicsList);
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

    #endregion
}