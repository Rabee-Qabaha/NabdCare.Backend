using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
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
    private readonly IAccessPolicy<Clinic> _policy;

    public ClinicService(
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<ClinicService> logger,
        IPermissionEvaluator permissionEvaluator,
        IAccessPolicy<Clinic> policy)
    {
        _clinicRepository = clinicRepository ?? throw new ArgumentNullException(nameof(clinicRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<ClinicResponseDto?> GetClinicByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        if (!await _policy.EvaluateAsync(_tenantContext, "read", clinic))
        {
            _logger.LogWarning("User {UserId} attempted to view clinic {ClinicId} without permission.", _userContext.GetCurrentUserId(), id);
            return null; 
        }

        return _mapper.Map<ClinicResponseDto>(clinic);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetAllClinicsPagedAsync(ClinicFilterRequestDto filters)
    {
        if (filters == null) throw new ArgumentNullException(nameof(filters));
        
        if (!_tenantContext.IsSuperAdmin)
             throw new UnauthorizedAccessException("Only SuperAdmin can view the global clinic list.");

        if (!_tenantContext.IsSuperAdmin) filters.IncludeDeleted = false;

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, Common.Constants.Permissions.Clinics.ViewAll, _userContext));

        var result = await _clinicRepository.GetAllPagedAsync(filters, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsByStatusPagedAsync(
        SubscriptionStatus status, PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Access denied.");

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, Common.Constants.Permissions.Clinics.ViewAll, _userContext));

        var result = await _clinicRepository.GetByStatusPagedAsync(status, pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetActiveClinicsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Access denied.");

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, Common.Constants.Permissions.Clinics.ViewAll, _userContext));

        var result = await _clinicRepository.GetActiveWithValidSubscriptionPagedAsync(pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiringSubscriptionsPagedAsync(
        int withinDays, PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Access denied.");

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, Common.Constants.Permissions.Clinics.ViewAll, _userContext));

        var result = await _clinicRepository.GetWithExpiringSubscriptionsPagedAsync(withinDays, pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> GetClinicsWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Access denied.");

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(query =>
            _permissionEvaluator.FilterClinics(query, Common.Constants.Permissions.Clinics.ViewAll, _userContext));

        var result = await _clinicRepository.GetWithExpiredSubscriptionsPagedAsync(pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<ClinicResponseDto>> SearchClinicsPagedAsync(string query, PaginationRequestDto pagination)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query required", nameof(query));
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Access denied.");

        var abacFilter = new Func<IQueryable<Clinic>, IQueryable<Clinic>>(q =>
            _permissionEvaluator.FilterClinics(q, Common.Constants.Permissions.Clinics.ViewAll, _userContext));

        var result = await _clinicRepository.SearchPagedAsync(query, pagination, abacFilter);
        return result.ToPaginatedDto<Clinic, ClinicResponseDto>(_mapper);
    }

    public async Task<ClinicStatisticsDto?> GetClinicStatisticsAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Access denied.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        var now = DateTime.UtcNow;
        var latestSubscription = clinic.Subscriptions?
            .Where(s => !s.IsDeleted && s.StartDate <= now && s.EndDate >= now)
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
            CurrentSubscription = latestSubscription != null ? _mapper.Map<SubscriptionResponseDto>(latestSubscription) : null,
            IsSubscriptionActive = latestSubscription != null && latestSubscription.Status == SubscriptionStatus.Active,
            DaysUntilExpiration = daysUntilExpiration,
            IsExpiringSoon = daysUntilExpiration is > 0 and <= 30,
            CreatedAt = clinic.CreatedAt
        };
    }

    public async Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can provision new clinics.");

        if (await _clinicRepository.ExistsByNameAsync(dto.Name))
            throw new DomainException($"Clinic name '{dto.Name}' is already taken.", ErrorCodes.DUPLICATE_NAME, "name");

        if (await _clinicRepository.ExistsBySlugAsync(dto.Slug))
            throw new DomainException($"Subdomain '{dto.Slug}' is already taken.", ErrorCodes.DUPLICATE_SLUG, "slug");

        if (await _clinicRepository.ExistsByEmailAsync(dto.Email))
            throw new DomainException($"Email '{dto.Email}' is already registered.", ErrorCodes.DUPLICATE_EMAIL, "email");

        var currentUserId = _userContext.GetCurrentUserId();

        var clinic = _mapper.Map<Clinic>(dto);
        clinic.Id = Guid.NewGuid();
        clinic.CreatedAt = DateTime.UtcNow;
        clinic.CreatedBy = currentUserId;
        clinic.IsDeleted = false;
        clinic.Status = SubscriptionStatus.Inactive;
        clinic.BranchCount = 1;

        var mainBranch = new Branch
        {
            Id = Guid.NewGuid(),
            ClinicId = clinic.Id,
            Name = "Main Branch",
            IsMain = true,
            IsActive = true,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,
            IsDeleted = false
        };

        clinic.Branches = new List<Branch> { mainBranch };
        clinic.Subscriptions = new List<Subscription>();

        var created = await _clinicRepository.CreateAsync(clinic);

        _logger.LogInformation("Clinic {Id} created (Status: Inactive)", created.Id);
        return _mapper.Map<ClinicResponseDto>(created);
    }

    public async Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        // CHECK 1: Edit Permission
        // We wrap the single clinic in a Queryable list to reuse the FilterClinics logic
        var editQuery = new List<Clinic> { clinic }.AsQueryable();
        var canEdit = _permissionEvaluator.FilterClinics(
            editQuery,
            Common.Constants.Permissions.Clinic.Edit,
            _userContext
        ).Any();

        if (!canEdit)
        {
            throw new UnauthorizedAccessException("You lack the basic permission to edit this clinic.");
        }

        // CHECK 2: Manage Financials Permission (only if settings changed)
        if (dto.Settings != null && (
            dto.Settings.Currency != clinic.Settings.Currency ||
            dto.Settings.ExchangeRateMarkupType != clinic.Settings.ExchangeRateMarkupType ||
            dto.Settings.ExchangeRateMarkupValue != clinic.Settings.ExchangeRateMarkupValue))
        {
            var financeQuery = new List<Clinic> { clinic }.AsQueryable();
            var canManageFinancials = _permissionEvaluator.FilterClinics(
                financeQuery,
                Common.Constants.Permissions.Clinic.ManageFinancials,
                _userContext
            ).Any();

            if (!canManageFinancials)
            {
                throw new UnauthorizedAccessException("You lack permission to manage financial settings.");
            }
        }

        if (await _clinicRepository.ExistsByNameAsync(dto.Name, id))
            throw new DomainException($"Name '{dto.Name}' is already taken.", ErrorCodes.DUPLICATE_NAME, "name");

        if (await _clinicRepository.ExistsBySlugAsync(dto.Slug, id))
            throw new DomainException($"Subdomain '{dto.Slug}' is already taken.", ErrorCodes.DUPLICATE_SLUG, "slug");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email, id))
            throw new DomainException($"Email '{dto.Email}' is already used by another clinic.", ErrorCodes.DUPLICATE_EMAIL, "email");

        _mapper.Map(dto, clinic);
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = _userContext.GetCurrentUserId();

        if (clinic.Branches.Any())
        {
            var mainBranch = clinic.Branches.FirstOrDefault(b => b.IsMain);
            if (mainBranch != null)
            {
                mainBranch.Email = clinic.Email;
                mainBranch.Phone = clinic.Phone;
                mainBranch.Address = clinic.Address;
                mainBranch.UpdatedAt = DateTime.UtcNow;
                mainBranch.UpdatedBy = _userContext.GetCurrentUserId();
            }
        }

        var updated = await _clinicRepository.UpdateAsync(clinic);
        return _mapper.Map<ClinicResponseDto>(updated);
    }

    public async Task<ClinicResponseDto?> UpdateClinicStatusAsync(Guid id, UpdateClinicStatusDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Only SuperAdmin can update status.");

        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) return null;

        clinic.Status = dto.Status;
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.UpdatedBy = _userContext.GetCurrentUserId();

        var sub = GetCurrentOrLatestSubscription(clinic);
        if (sub != null)
        {
            sub.Status = dto.Status;
            sub.UpdatedAt = DateTime.UtcNow;
            sub.UpdatedBy = _userContext.GetCurrentUserId();
        }

        await _clinicRepository.UpdateAsync(clinic);
        return _mapper.Map<ClinicResponseDto>(clinic);
    }

    public async Task<ClinicResponseDto?> ActivateClinicAsync(Guid id)
    {
        return await UpdateClinicStatusAsync(id, new UpdateClinicStatusDto { Status = SubscriptionStatus.Active });
    }

    public async Task<ClinicResponseDto?> SuspendClinicAsync(Guid id)
    {
        return await UpdateClinicStatusAsync(id, new UpdateClinicStatusDto { Status = SubscriptionStatus.Suspended });
    }

    public async Task<bool> SoftDeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Only SuperAdmin can delete clinics.");
        
        return await _clinicRepository.SoftDeleteAsync(id);
    }

    public async Task<bool> DeleteClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Only SuperAdmin can hard delete clinics.");
        
        return await _clinicRepository.DeleteAsync(id);
    }

    public async Task<bool> RestoreClinicAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (!_tenantContext.IsSuperAdmin) throw new UnauthorizedAccessException("Only SuperAdmin can restore clinics.");
        
        return await _clinicRepository.RestoreAsync(id);
    }

    private Subscription? GetCurrentOrLatestSubscription(Clinic clinic)
    {
        if (clinic.Subscriptions == null || !clinic.Subscriptions.Any())
            return null;

        var now = DateTime.UtcNow;
        var activeSubscriptions = clinic.Subscriptions.Where(s => !s.IsDeleted).ToList();
        
        var current = activeSubscriptions.FirstOrDefault(s => s.StartDate <= now && s.EndDate >= now);
        
        return current ?? activeSubscriptions.OrderByDescending(s => s.StartDate).FirstOrDefault();
    }
}