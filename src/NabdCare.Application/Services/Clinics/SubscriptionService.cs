using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

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
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto)
    {
        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");

        var subscription = _mapper.Map<Subscription>(dto);

        try
        {
            var created = await _repository.CreateAsync(subscription);
            _logger.LogInformation("Subscription {SubscriptionId} created for Clinic {ClinicId}.", created.Id, created.ClinicId);
            return _mapper.Map<SubscriptionResponseDto>(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subscription for Clinic {ClinicId}.", dto.ClinicId);
            throw;
        }
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        var subscription = await _repository.GetByIdAsync(id, includePayments);
        return subscription == null ? null : _mapper.Map<SubscriptionResponseDto>(subscription);
    }

    public async Task<IEnumerable<SubscriptionResponseDto>> GetByClinicIdAsync(Guid clinicId, bool includePayments = false)
    {
        var subscriptions = await _repository.GetByClinicIdAsync(clinicId, includePayments);
        return _mapper.Map<IEnumerable<SubscriptionResponseDto>>(subscriptions);
    }

    public async Task<IEnumerable<SubscriptionResponseDto>> GetPagedAsync(int page, int pageSize, bool includePayments = false)
    {
        var subscriptions = await _repository.GetPagedAsync(page, pageSize, includePayments);
        return _mapper.Map<IEnumerable<SubscriptionResponseDto>>(subscriptions);
    }

    public async Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto)
    {
        var existing = await _repository.GetByIdAsync(id, includePayments: false);
        if (existing == null) return null;

        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");

        _mapper.Map(dto, existing);

        try
        {
            var updated = await _repository.UpdateAsync(existing);
            _logger.LogInformation("Subscription {SubscriptionId} updated.", id);
            return _mapper.Map<SubscriptionResponseDto>(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}.", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteSubscriptionAsync(Guid id)
    {
        var success = await _repository.SoftDeleteAsync(id);
        if (success) _logger.LogInformation("Subscription {SubscriptionId} soft-deleted.", id);
        return success;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id)
    {
        var success = await _repository.DeleteAsync(id);
        if (success) _logger.LogInformation("Subscription {SubscriptionId} permanently deleted.", id);
        return success;
    }
}
