using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Clinics;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository repository,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException($"StartDate must be before EndDate. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.StartDate));

        _logger.LogInformation("Creating subscription for clinic {ClinicId}. Start: {StartDate}, End: {EndDate}",
            dto.ClinicId, dto.StartDate, dto.EndDate);

        var entity = _mapper.Map<Subscription>(dto);
        var created = await _repository.CreateAsync(entity);

        _logger.LogInformation("Subscription {SubscriptionId} created for clinic {ClinicId}",
            created.Id, created.ClinicId);

        return _mapper.Map<SubscriptionResponseDto>(created);
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogDebug("Retrieving subscription {SubscriptionId}", id);

        var sub = await _repository.GetByIdAsync(id, includePayments);
        if (sub == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found. Error code: {ErrorCode}", id, ErrorCodes.NOT_FOUND);
            return null;
        }

        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(clinicId));

        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        _logger.LogInformation("Retrieving paginated subscriptions for clinic {ClinicId} (Limit={Limit})",
            clinicId, pagination.Limit);

        var result = await _repository.GetByClinicIdPagedAsync(clinicId, pagination, includePayments, abacFilter);

        _logger.LogInformation("Retrieved {Count} subscriptions for clinic {ClinicId}",
            result.Items.Count(), clinicId);

        return new PaginatedResult<SubscriptionResponseDto>
        {
            Items = _mapper.Map<IEnumerable<SubscriptionResponseDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        _logger.LogInformation("Retrieving all paginated subscriptions (Limit={Limit})", pagination.Limit);

        var result = await _repository.GetAllPagedAsync(pagination, includePayments, abacFilter);

        _logger.LogInformation("Retrieved {Count} total subscriptions", result.Items.Count());

        return new PaginatedResult<SubscriptionResponseDto>
        {
            Items = _mapper.Map<IEnumerable<SubscriptionResponseDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        _logger.LogInformation("Retrieving paginated subscriptions (Limit={Limit})", pagination.Limit);

        var result = await _repository.GetPagedAsync(pagination, includePayments, abacFilter);

        _logger.LogInformation("Retrieved {Count} paginated subscriptions", result.Items.Count());

        return new PaginatedResult<SubscriptionResponseDto>
        {
            Items = _mapper.Map<IEnumerable<SubscriptionResponseDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException($"StartDate must be before EndDate. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.StartDate));

        _logger.LogInformation("Updating subscription {SubscriptionId}", id);

        var existing = await _repository.GetByIdAsync(id, includePayments: false);
        if (existing == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for update. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return null;
        }

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);

        _logger.LogInformation("Subscription {SubscriptionId} updated successfully", id);

        return _mapper.Map<SubscriptionResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteSubscriptionAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogInformation("Soft deleting subscription {SubscriptionId}", id);

        var ok = await _repository.SoftDeleteAsync(id);
        
        if (ok)
            _logger.LogInformation("Subscription {SubscriptionId} soft deleted successfully", id);
        else
            _logger.LogWarning("Subscription {SubscriptionId} not found for soft deletion. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);

        return ok;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogWarning("Permanently deleting subscription {SubscriptionId}", id);

        var ok = await _repository.DeleteAsync(id);

        if (ok)
            _logger.LogWarning("Subscription {SubscriptionId} permanently deleted", id);
        else
            _logger.LogWarning("Subscription {SubscriptionId} not found for permanent deletion. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);

        return ok;
    }

    public async Task<SubscriptionResponseDto?> UpdateSubscriptionStatusAsync(Guid id, SubscriptionStatus newStatus)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogInformation("Updating subscription {SubscriptionId} status to {Status}", id, newStatus);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return null;
        }

        existing.Status = newStatus;
        existing.UpdatedAt = DateTime.UtcNow;

        var saved = await _repository.UpdateStatusAsync(existing);

        _logger.LogInformation("Subscription {SubscriptionId} status updated to {Status}", id, newStatus);

        return _mapper.Map<SubscriptionResponseDto>(saved);
    }

    public async Task<int> ProcessAutoRenewalsAsync(DateTime nowUtc)
    {
        _logger.LogInformation("Starting auto-renewal processing at {TimeUtc}", nowUtc);

        var candidates = await _repository.GetAutoRenewCandidatesAsync(nowUtc);
        var createdCount = 0;

        foreach (var old in candidates)
        {
            // Mark old as expired (if not already)
            if (old.Status != SubscriptionStatus.Expired)
            {
                old.Status = SubscriptionStatus.Expired;
                old.UpdatedAt = nowUtc;
                await _repository.UpdateStatusAsync(old);
            }

            // Create new subscription – same plan & fee
            var start = nowUtc;
            var end = old.Type switch
            {
                SubscriptionType.Monthly => start.AddMonths(1),
                SubscriptionType.Yearly => start.AddYears(1),
                SubscriptionType.Lifetime => start.AddYears(100),
                _ => throw new ArgumentOutOfRangeException($"Invalid subscription type {old.Type}. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(old.Type))
            };

            var next = new Subscription
            {
                ClinicId = old.ClinicId,
                StartDate = start,
                EndDate = end,
                Type = old.Type,
                Fee = old.Fee,
                Status = SubscriptionStatus.Active,
                AutoRenew = old.AutoRenew,
                GracePeriodDays = old.GracePeriodDays,
                PreviousSubscriptionId = old.Id,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            await _repository.CreateAsync(next);
            createdCount++;

            _logger.LogDebug("Auto-renewed subscription for clinic {ClinicId}. Old: {OldId}, New: {NewId}",
                old.ClinicId, old.Id, next.Id);
        }

        _logger.LogInformation("Auto-renewal job completed. Created {Count} new subscriptions", createdCount);

        return createdCount;
    }

    public async Task<SubscriptionResponseDto?> ToggleAutoRenewAsync(Guid id, bool enable)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogInformation("Toggling AutoRenew for subscription {SubscriptionId} to {Enable}", id, enable);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found. Error code: {ErrorCode}",
                id, ErrorCodes.NOT_FOUND);
            return null;
        }

        existing.AutoRenew = enable;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);

        _logger.LogInformation("AutoRenew for subscription {SubscriptionId} set to {Enable}", id, enable);

        return _mapper.Map<SubscriptionResponseDto>(updated);
    }
    
    public async Task<SubscriptionResponseDto> RenewSubscriptionAsync(Guid oldSubscriptionId, SubscriptionType type)
    {
        if (oldSubscriptionId == Guid.Empty)
            throw new ArgumentException($"Old subscription ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(oldSubscriptionId));

        _logger.LogInformation("Starting manual renewal for subscription {OldSubscriptionId} with type {Type}",
            oldSubscriptionId, type);

        var oldSub = await _repository.GetByIdAsync(oldSubscriptionId);
        if (oldSub == null)
        {
            _logger.LogWarning("Old subscription {OldSubscriptionId} not found. Error code: {ErrorCode}",
                oldSubscriptionId, ErrorCodes.NOT_FOUND);
            throw new KeyNotFoundException($"Subscription {oldSubscriptionId} not found. Error code: {ErrorCodes.NOT_FOUND}");
        }

        // Calculate new subscription dates
        var startDate = DateTime.UtcNow;
        var endDate = type switch
        {
            SubscriptionType.Monthly => startDate.AddMonths(1),
            SubscriptionType.Yearly => startDate.AddYears(1),
            SubscriptionType.Lifetime => startDate.AddYears(100),
            _ => throw new ArgumentOutOfRangeException($"Invalid subscription type {type}. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(type))
        };

        // Mark old subscription as expired
        if (oldSub.Status != SubscriptionStatus.Expired)
        {
            oldSub.Status = SubscriptionStatus.Expired;
            oldSub.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateStatusAsync(oldSub);

            _logger.LogDebug("Marked old subscription {OldSubscriptionId} as Expired", oldSubscriptionId);
        }

        // Create new subscription
        var newSub = new Subscription
        {
            ClinicId = oldSub.ClinicId,
            StartDate = startDate,
            EndDate = endDate,
            Status = SubscriptionStatus.Active,
            Type = type,
            Fee = oldSub.Fee,
            AutoRenew = oldSub.AutoRenew,
            GracePeriodDays = oldSub.GracePeriodDays,
            PreviousSubscriptionId = oldSub.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(newSub);

        _logger.LogInformation("Manual renewal completed. Old: {OldId}, New: {NewId}, Type: {Type}, Clinic: {ClinicId}",
            oldSub.Id, created.Id, type, oldSub.ClinicId);

        return _mapper.Map<SubscriptionResponseDto>(created);
    }
}