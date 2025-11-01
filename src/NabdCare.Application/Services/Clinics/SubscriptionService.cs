// NabdCare.Application/Services/Clinics/SubscriptionService.cs
using AutoMapper;
using Microsoft.Extensions.Logging;
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
        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");

        var entity = _mapper.Map<Subscription>(dto);
        var created = await _repository.CreateAsync(entity);

        _logger.LogInformation("Subscription {SubscriptionId} created for Clinic {ClinicId}.",
            created.Id, created.ClinicId);

        return _mapper.Map<SubscriptionResponseDto>(created);
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        var sub = await _repository.GetByIdAsync(id, includePayments);
        return sub == null ? null : _mapper.Map<SubscriptionResponseDto>(sub);
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        var result = await _repository.GetByClinicIdPagedAsync(clinicId, pagination, includePayments, abacFilter);

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
        var result = await _repository.GetAllPagedAsync(pagination, includePayments, abacFilter);

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
        var result = await _repository.GetPagedAsync(pagination, includePayments, abacFilter);

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
        if (id == Guid.Empty) throw new ArgumentException("Invalid subscription ID.", nameof(id));
        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");

        var existing = await _repository.GetByIdAsync(id, includePayments: false);
        if (existing == null) return null;

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        _logger.LogInformation("Subscription {SubscriptionId} updated.", id);

        return _mapper.Map<SubscriptionResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteSubscriptionAsync(Guid id)
    {
        var ok = await _repository.SoftDeleteAsync(id);
        if (ok) _logger.LogInformation("Subscription {Id} soft deleted.", id);
        return ok;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id)
    {
        var ok = await _repository.DeleteAsync(id);
        if (ok) _logger.LogWarning("Subscription {Id} permanently deleted.", id);
        return ok;
    }

    public async Task<SubscriptionResponseDto?> UpdateSubscriptionStatusAsync(Guid id, SubscriptionStatus newStatus)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return null;

        existing.Status = newStatus;
        existing.UpdatedAt = DateTime.UtcNow;

        var saved = await _repository.UpdateStatusAsync(existing);
        return _mapper.Map<SubscriptionResponseDto>(saved);
    }

    public async Task<int> ProcessAutoRenewalsAsync(DateTime nowUtc)
    {
        var candidates = await _repository.GetAutoRenewCandidatesAsync(nowUtc);
        var createdCount = 0;

        foreach (var old in candidates)
        {
            // mark old as expired (if not already)
            if (old.Status != SubscriptionStatus.Expired)
            {
                old.Status = SubscriptionStatus.Expired;
                old.UpdatedAt = nowUtc;
                await _repository.UpdateStatusAsync(old);
            }

            // create new subscription – same plan & fee
            var start = nowUtc; // allow gaps: resume from "now"
            var end = old.Type switch
            {
                SubscriptionType.Monthly => start.AddMonths(1),
                SubscriptionType.Yearly => start.AddYears(1),
                SubscriptionType.Lifetime => start.AddYears(100),
                _ => throw new ArgumentOutOfRangeException(nameof(old.Type))
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
        }

        _logger.LogInformation("Auto-renewal job created {Count} new subscriptions.", createdCount);
        return createdCount;
    }

    public async Task<SubscriptionResponseDto?> ToggleAutoRenewAsync(Guid id, bool enable)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return null;

        existing.AutoRenew = enable;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        _logger.LogInformation("AutoRenew for subscription {Id} set to {Enable}", id, enable);

        return _mapper.Map<SubscriptionResponseDto>(updated);
    }
    
    public async Task<SubscriptionResponseDto> RenewSubscriptionAsync(Guid oldSubscriptionId, SubscriptionType type)
    {
        var oldSub = await _repository.GetByIdAsync(oldSubscriptionId);
        if (oldSub == null)
            throw new KeyNotFoundException($"Subscription {oldSubscriptionId} not found.");

        // allow gaps: start from now
        var startDate = DateTime.UtcNow;
        var endDate = type switch
        {
            SubscriptionType.Monthly => startDate.AddMonths(1),
            SubscriptionType.Yearly  => startDate.AddYears(1),
            SubscriptionType.Lifetime => startDate.AddYears(100),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        if (oldSub.Status != SubscriptionStatus.Expired)
        {
            oldSub.Status = SubscriptionStatus.Expired;
            oldSub.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateStatusAsync(oldSub);
        }

        var newSub = new Subscription
        {
            ClinicId = oldSub.ClinicId,
            StartDate = startDate,
            EndDate = endDate,
            Status = SubscriptionStatus.Active,
            Type = type,
            Fee = oldSub.Fee, // or resolve from pricing table if needed
            AutoRenew = oldSub.AutoRenew,
            GracePeriodDays = oldSub.GracePeriodDays,
            PreviousSubscriptionId = oldSub.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(newSub);
        _logger.LogInformation("Manual renewal: Old={OldId}, New={NewId}, Type={Type}", oldSub.Id, created.Id, type);

        return _mapper.Map<SubscriptionResponseDto>(created);
    }
}