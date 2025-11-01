using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Clinics;

/// <summary>
/// Production-ready clinic service following clean architecture.
/// Integrates ABAC filters for secure data visibility.
/// </summary>
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
        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetAllPagedAsync(pagination, abacFilter);

        _logger.LogInformation("User {UserId} retrieved {Count} clinics (HasMore={HasMore})",
            _userContext.GetCurrentUserId(), result.Items.Count(), result.HasMore);

        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsByStatusPagedAsync(
        SubscriptionStatus status, PaginationRequestDto pagination)
    {
        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetByStatusPagedAsync(status, pagination, abacFilter);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetActiveClinicsPagedAsync(PaginationRequestDto pagination)
    {
        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetActiveWithValidSubscriptionPagedAsync(pagination, abacFilter);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsPagedAsync(
        int withinDays, PaginationRequestDto pagination)
    {
        if (withinDays < 1 || withinDays > 365)
            throw new ArgumentException("Days must be between 1 and 365", nameof(withinDays));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetWithExpiringSubscriptionsPagedAsync(withinDays, pagination, abacFilter);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination)
    {
        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetWithExpiredSubscriptionsPagedAsync(pagination, abacFilter);
        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> SearchClinicsPagedAsync(string query, PaginationRequestDto pagination)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty", nameof(query));

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(q =>
            _permissionEvaluator.FilterClinics(q, "Clinics.View", _userContext));

        var result = await _clinicRepository.SearchPagedAsync(query, pagination, abacFilter);
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

        if (await _clinicRepository.ExistsByNameAsync(dto.Name))
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists");

        var clinic = _mapper.Map<Clinic>(dto);
        clinic.Id = Guid.NewGuid();
        clinic.Status = dto.Status;
        clinic.CreatedAt = DateTime.UtcNow;
        clinic.CreatedBy = currentUserId;
        clinic.IsDeleted = false;

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

        _logger.LogInformation("User {CurrentUserId} created clinic {ClinicId} ({ClinicName})",
            currentUserId, created.Id, created.Name);

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
            return null;

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
            throw new UnauthorizedAccessException("You can only update your own clinic");

        if (await _clinicRepository.ExistsByNameAsync(dto.Name, id))
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email, id))
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists");

        clinic.Name = dto.Name.Trim();
        clinic.Email = dto.Email?.Trim();
        clinic.Phone = dto.Phone?.Trim();
        clinic.Address = dto.Address?.Trim();
        clinic.BranchCount = dto.BranchCount;
        clinic.Status = dto.Status;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

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

        var updated = await _clinicRepository.UpdateAsync(clinic);
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
            throw new UnauthorizedAccessException("Only SuperAdmin can update clinic status");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
            return null;

        clinic.Status = dto.Status;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

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
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> ActivateClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can activate clinics");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
            return null;

        clinic.Status = SubscriptionStatus.Active;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

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
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> SuspendClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can suspend clinics");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
            return null;

        clinic.Status = SubscriptionStatus.Suspended;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = currentUserId;

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
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can delete clinics");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
            return false;

        return await _clinicRepository.SoftDeleteAsync(id);
    }

    public async Task<bool> DeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete clinics");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
            return false;

        return await _clinicRepository.DeleteAsync(id);
    }

    #endregion

    #region STATISTICS

    public async Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(id));

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can view clinic statistics");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
            return null;

        var now = DateTime.UtcNow;
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        var daysUntilExpiration = latestSubscription != null
            ? (int)(latestSubscription.EndDate - now).TotalDays
            : 0;

        return new ClinicStatisticsDto
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
    }

    #endregion

    #region HELPERS

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