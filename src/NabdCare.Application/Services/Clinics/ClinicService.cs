using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;
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

    public async Task<PaginatedResult<ClinicResponseDto>> GetAllClinicsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} retrieving paginated clinics (Limit={Limit})", currentUserId, pagination.Limit);

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetAllPagedAsync(pagination, abacFilter);

        _logger.LogInformation("User {UserId} retrieved {Count} clinics (HasMore={HasMore}, TotalCount={TotalCount})",
            currentUserId, result.Items.Count(), result.HasMore, result.TotalCount);

        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsByStatusPagedAsync(
        SubscriptionStatus status, PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} retrieving clinics by status {Status} (Limit={Limit})",
            currentUserId, status, pagination.Limit);

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetByStatusPagedAsync(status, pagination, abacFilter);

        _logger.LogInformation("Retrieved {Count} clinics with status {Status}", result.Items.Count(), status);

        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetActiveClinicsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} retrieving active clinics (Limit={Limit})", currentUserId, pagination.Limit);

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetActiveWithValidSubscriptionPagedAsync(pagination, abacFilter);

        _logger.LogInformation("Retrieved {Count} active clinics with valid subscriptions", result.Items.Count());

        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsPagedAsync(
        int withinDays, PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        if (withinDays < 1 || withinDays > 365)
            throw new ArgumentException($"Days must be between 1 and 365. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(withinDays));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} retrieving clinics with subscriptions expiring within {Days} days",
            currentUserId, withinDays);

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetWithExpiringSubscriptionsPagedAsync(withinDays, pagination, abacFilter);

        _logger.LogInformation("Retrieved {Count} clinics with expiring subscriptions", result.Items.Count());

        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} retrieving clinics with expired subscriptions", currentUserId);

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, "Clinics.View", _userContext));

        var result = await _clinicRepository.GetWithExpiredSubscriptionsPagedAsync(pagination, abacFilter);

        _logger.LogInformation("Retrieved {Count} clinics with expired subscriptions", result.Items.Count());

        return MapPaginated(result);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> SearchClinicsPagedAsync(string query, PaginationRequestDto pagination)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException($"Search query cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(query));

        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} searching clinics with query '{Query}'", currentUserId, query);

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(q =>
            _permissionEvaluator.FilterClinics(q, "Clinics.View", _userContext));

        var result = await _clinicRepository.SearchPagedAsync(query, pagination, abacFilter);

        _logger.LogInformation("Search returned {Count} clinics matching '{Query}'", result.Items.Count(), query);

        return MapPaginated(result);
    }

    public async Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        ValidateCreateClinicDto(dto);

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} creating clinic {ClinicName}", currentUserId, dto.Name);

        if (await _clinicRepository.ExistsByNameAsync(dto.Name))
        {
            _logger.LogWarning("Clinic name {ClinicName} already exists. Error code: {ErrorCode}",
                dto.Name, ErrorCodes.DUPLICATE_RESOURCE);
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email))
        {
            _logger.LogWarning("Clinic email {Email} already exists. Error code: {ErrorCode}",
                dto.Email, ErrorCodes.DUPLICATE_RESOURCE);
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

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

        _logger.LogInformation("User {CurrentUserId} created clinic {ClinicId} with name {ClinicName}",
            currentUserId, created.Id, created.Name);

        return _mapper.Map<ClinicResponseDto>(created);
    }

    public async Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        ValidateUpdateClinicDto(dto);

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} updating clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for update. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return null;
        }

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != id)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to update clinic {ClinicId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"You can only update your own clinic. Error code: {ErrorCodes.FORBIDDEN}");
        }

        if (await _clinicRepository.ExistsByNameAsync(dto.Name, id))
        {
            _logger.LogWarning("Clinic name {ClinicName} already exists. Error code: {ErrorCode}",
                dto.Name, ErrorCodes.DUPLICATE_RESOURCE);
            throw new InvalidOperationException($"A clinic with name '{dto.Name}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email, id))
        {
            _logger.LogWarning("Clinic email {Email} already exists. Error code: {ErrorCode}",
                dto.Email, ErrorCodes.DUPLICATE_RESOURCE);
            throw new InvalidOperationException($"A clinic with email '{dto.Email}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

        clinic.Name = dto.Name.Trim();
        clinic.Email = dto.Email.Trim();
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

        _logger.LogInformation("User {CurrentUserId} updated clinic {ClinicId}", currentUserId, id);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> UpdateClinicStatusAsync(Guid id, UpdateClinicStatusDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to update clinic status. Error code: {ErrorCode}",
                currentUserId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Only SuperAdmin can update clinic status. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} updating clinic {ClinicId} status to {Status}",
            currentUserId, id, dto.Status);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for status update. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return null;
        }

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

        _logger.LogInformation("SuperAdmin {CurrentUserId} updated clinic {ClinicId} status to {Status}",
            currentUserId, id, dto.Status);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> ActivateClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to activate clinic. Error code: {ErrorCode}",
                currentUserId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Only SuperAdmin can activate clinics. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} activating clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for activation. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
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

        _logger.LogInformation("SuperAdmin {CurrentUserId} activated clinic {ClinicId}", currentUserId, id);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> SuspendClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to suspend clinic. Error code: {ErrorCode}",
                currentUserId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Only SuperAdmin can suspend clinics. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} suspending clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for suspension. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
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

        _logger.LogInformation("SuperAdmin {CurrentUserId} suspended clinic {ClinicId}", currentUserId, id);

        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to soft delete clinic. Error code: {ErrorCode}",
                currentUserId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Only SuperAdmin can delete clinics. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} soft deleting clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for soft deletion. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return false;
        }

        var result = await _clinicRepository.SoftDeleteAsync(id);

        if (result)
            _logger.LogInformation("SuperAdmin {CurrentUserId} soft deleted clinic {ClinicId}", currentUserId, id);

        return result;
    }

    public async Task<bool> DeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to permanently delete clinic. Error code: {ErrorCode}",
                currentUserId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Only SuperAdmin can permanently delete clinics. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogWarning("SuperAdmin {CurrentUserId} permanently deleting clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for permanent deletion. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return false;
        }

        var result = await _clinicRepository.DeleteAsync(id);

        if (result)
            _logger.LogWarning("SuperAdmin {CurrentUserId} permanently deleted clinic {ClinicId}", currentUserId, id);

        return result;
    }

    public async Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to view clinic statistics. Error code: {ErrorCode}",
                currentUserId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Only SuperAdmin can view clinic statistics. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieving statistics for clinic {ClinicId}", currentUserId, id);

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null)
        {
            _logger.LogWarning("Clinic {ClinicId} not found for statistics. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
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
            IsExpiringSoon = daysUntilExpiration is > 0 and <= 30,
            CreatedAt = clinic.CreatedAt
        };

        _logger.LogInformation("Retrieved statistics for clinic {ClinicId}: Status={Status}, DaysUntilExpiration={Days}",
            id, clinic.Status, daysUntilExpiration);

        return statistics;
    }

    private static void ValidateCreateClinicDto(CreateClinicRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException($"Clinic name is required. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.Name));

        if (dto.SubscriptionStartDate >= dto.SubscriptionEndDate)
            throw new ArgumentException($"Subscription start date must be before end date. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.SubscriptionStartDate));

        if (dto.SubscriptionFee < 0)
            throw new ArgumentException($"Subscription fee cannot be negative. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.SubscriptionFee));

        if (dto.BranchCount < 1)
            throw new ArgumentException($"Branch count must be at least 1. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.BranchCount));
    }

    private void ValidateUpdateClinicDto(UpdateClinicRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException($"Clinic name is required. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.Name));

        if (dto.SubscriptionStartDate >= dto.SubscriptionEndDate)
            throw new ArgumentException($"Subscription start date must be before end date. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.SubscriptionStartDate));

        if (dto.SubscriptionFee < 0)
            throw new ArgumentException($"Subscription fee cannot be negative. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.SubscriptionFee));

        if (dto.BranchCount < 1)
            throw new ArgumentException($"Branch count must be at least 1. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.BranchCount));
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
}