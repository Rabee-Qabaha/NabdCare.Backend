using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Clinics;

public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinicRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(IClinicRepository clinicRepository, IMapper mapper, ILogger<ClinicService> logger)
    {
        _clinicRepository = clinicRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto)
    {
        // Business validations
        if (dto.SubscriptionStartDate >= dto.SubscriptionEndDate)
            throw new ArgumentException("SubscriptionStartDate must be before SubscriptionEndDate.");

        if (await _clinicRepository.ExistsByNameAsync(dto.Name))
            throw new ArgumentException("A clinic with the same name already exists.");

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _clinicRepository.ExistsByEmailAsync(dto.Email))
            throw new ArgumentException("A clinic with the same email already exists.");

        // Map DTO to entity
        var clinic = _mapper.Map<Clinic>(dto);

        // Create initial subscription
        clinic.Subscriptions = new List<Subscription>
        {
            new Subscription
            {
                StartDate = dto.SubscriptionStartDate,
                EndDate = dto.SubscriptionEndDate,
                Fee = dto.SubscriptionFee,
                Type = dto.SubscriptionType,
                Status = dto.Status
            }
        };

        try
        {
            var created = await _clinicRepository.CreateAsync(clinic);
            _logger.LogInformation("Clinic {ClinicId} created (Name={Name}).", created.Id, created.Name);
            return _mapper.Map<ClinicResponseDto>(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create clinic (Name={Name}).", dto.Name);
            throw;
        }
    }

    public async Task<ClinicResponseDto?> GetClinicByIdAsync(Guid id)
    {
        var clinic = await _clinicRepository.GetByIdAsync(id);
        return clinic == null ? null : _mapper.Map<ClinicResponseDto>(clinic);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetAllClinicsAsync()
    {
        var clinics = await _clinicRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinics);
    }

    public async Task<IEnumerable<ClinicResponseDto>> GetPagedClinicsAsync(int page, int pageSize)
    {
        var clinics = await _clinicRepository.GetPagedAsync(page, pageSize);
        return _mapper.Map<IEnumerable<ClinicResponseDto>>(clinics);
    }

    public async Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto)
    {
        var existing = await _clinicRepository.GetByIdAsync(id);
        if (existing == null) return null;

        if (dto.SubscriptionStartDate >= dto.SubscriptionEndDate)
            throw new ArgumentException("SubscriptionStartDate must be before SubscriptionEndDate.");

        if (!string.Equals(existing.Name, dto.Name, StringComparison.OrdinalIgnoreCase)
            && await _clinicRepository.ExistsByNameAsync(dto.Name))
            throw new ArgumentException("A clinic with the same name already exists.");

        if (!string.IsNullOrWhiteSpace(dto.Email)
            && !string.Equals(existing.Email, dto.Email, StringComparison.OrdinalIgnoreCase)
            && await _clinicRepository.ExistsByEmailAsync(dto.Email))
            throw new ArgumentException("A clinic with the same email already exists.");

        // Update main clinic info
        _mapper.Map(dto, existing);

        // Update subscription: latest or create new
        var latestSubscription = existing.Subscriptions?.OrderByDescending(s => s.StartDate).FirstOrDefault();
        if (latestSubscription != null)
        {
            latestSubscription.StartDate = dto.SubscriptionStartDate;
            latestSubscription.EndDate = dto.SubscriptionEndDate;
            latestSubscription.Fee = dto.SubscriptionFee;
            latestSubscription.Type = dto.SubscriptionType;
            latestSubscription.Status = dto.Status;
        }
        else
        {
            existing.Subscriptions = new List<Subscription>
            {
                new Subscription
                {
                    StartDate = dto.SubscriptionStartDate,
                    EndDate = dto.SubscriptionEndDate,
                    Fee = dto.SubscriptionFee,
                    Type = dto.SubscriptionType,
                    Status = dto.Status
                }
            };
        }

        try
        {
            var updated = await _clinicRepository.UpdateAsync(existing);
            _logger.LogInformation("Clinic {ClinicId} updated (Name={Name}).", id, updated.Name);
            return _mapper.Map<ClinicResponseDto>(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update clinic {ClinicId}.", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteClinicAsync(Guid id)
    {
        var success = await _clinicRepository.SoftDeleteAsync(id);
        if (success) _logger.LogInformation("Clinic {ClinicId} soft-deleted.", id);
        return success;
    }

    public async Task<bool> DeleteClinicAsync(Guid id)
    {
        var success = await _clinicRepository.DeleteAsync(id);
        if (success) _logger.LogInformation("Clinic {ClinicId} permanently deleted.", id);
        return success;
    }
}
