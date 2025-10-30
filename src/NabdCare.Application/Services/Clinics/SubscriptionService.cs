using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Clinics;

/// <summary>
/// Handles subscription business logic (validation, mapping, and error logging).
/// Delegates data operations to SubscriptionRepository.
/// </summary>
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
            throw new ArgumentException("StartDate must be before EndDate.");

        var subscription = _mapper.Map<Subscription>(dto);

        var created = await _repository.CreateAsync(subscription);

        _logger.LogInformation("Subscription {SubscriptionId} created for Clinic {ClinicId}.", created.Id, created.ClinicId);
        return _mapper.Map<SubscriptionResponseDto>(created);
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        var subscription = await _repository.GetByIdAsync(id, includePayments);
        return subscription == null ? null : _mapper.Map<SubscriptionResponseDto>(subscription);
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto pagination, bool includePayments = false)
    {
        var result = await _repository.GetByClinicIdPagedAsync(clinicId, pagination, includePayments);
        return new PaginatedResult<SubscriptionResponseDto>
        {
            Items = _mapper.Map<IEnumerable<SubscriptionResponseDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination, bool includePayments = false)
    {
        var result = await _repository.GetAllPagedAsync(pagination, includePayments);
        return new PaginatedResult<SubscriptionResponseDto>
        {
            Items = _mapper.Map<IEnumerable<SubscriptionResponseDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetPagedAsync(PaginationRequestDto pagination, bool includePayments = false)
    {
        var result = await _repository.GetPagedAsync(pagination, includePayments);
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
            throw new ArgumentException("Invalid subscription ID.", nameof(id));

        var existing = await _repository.GetByIdAsync(id, includePayments: false);
        if (existing == null)
            return null;

        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");

        _mapper.Map(dto, existing);

        var updated = await _repository.UpdateAsync(existing);

        _logger.LogInformation("Subscription {SubscriptionId} updated.", id);
        return _mapper.Map<SubscriptionResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteSubscriptionAsync(Guid id)
    {
        var success = await _repository.SoftDeleteAsync(id);
        if (success)
            _logger.LogInformation("Subscription {SubscriptionId} soft deleted.", id);
        else
            _logger.LogWarning("Failed to soft delete subscription {SubscriptionId}. Not found.", id);

        return success;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id)
    {
        var success = await _repository.DeleteAsync(id);
        if (success)
            _logger.LogWarning("Subscription {SubscriptionId} permanently deleted.", id);
        else
            _logger.LogWarning("Failed to permanently delete subscription {SubscriptionId}. Not found.", id);

        return success;
    }
}