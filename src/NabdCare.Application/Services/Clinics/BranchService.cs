using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Application.Validator.Clinics.Branches;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Clinics;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<BranchService> _logger;

    public BranchService(
        IBranchRepository branchRepository,
        ISubscriptionRepository subscriptionRepository,
        IMapper mapper,
        ILogger<BranchService> logger)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a list of branches. 
    /// If clinicId is provided, returns branches for that specific clinic.
    /// If null, behavior depends on Repository (usually all branches for SuperAdmin).
    /// </summary>
    public async Task<List<BranchResponseDto>> GetBranchesAsync(Guid? clinicId = null)
    {
        // Pass-through to repository which handles the filtering logic
        var branches = await _branchRepository.GetListAsync(clinicId);
        return _mapper.Map<List<BranchResponseDto>>(branches);
    }

    public async Task<BranchResponseDto?> GetBranchByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID is required", nameof(id));
        
        var branch = await _branchRepository.GetByIdAsync(id);
        return branch == null ? null : _mapper.Map<BranchResponseDto>(branch);
    }

    public async Task<BranchResponseDto> CreateBranchAsync(CreateBranchRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        // =================================================================
        // 🛑 GATEKEEPER: ENFORCE SUBSCRIPTION LIMITS
        // =================================================================
        var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(dto.ClinicId);

        if (activeSub == null)
        {
            throw new InvalidOperationException("No active subscription found. Please subscribe to a plan to add branches.");
        }

        var currentCount = await _branchRepository.CountByClinicIdAsync(dto.ClinicId);

        if (currentCount >= activeSub.MaxBranches)
        {
            _logger.LogWarning("Limit Reached: Clinic {ClinicId} has {Count}/{Max} branches.", 
                dto.ClinicId, currentCount, activeSub.MaxBranches);

            throw new InvalidOperationException(
                $"You have reached the limit of {activeSub.MaxBranches} branches.\n" +
                $"Plan: {activeSub.IncludedBranchesSnapshot} | Purchased: {activeSub.PurchasedBranches} | Bonus: {activeSub.BonusBranches}\n" +
                "Please upgrade your subscription to add more branches.");
        }
        // =================================================================

        var branch = _mapper.Map<Branch>(dto);
        branch.Id = Guid.NewGuid();
        branch.CreatedAt = DateTime.UtcNow;
        branch.IsDeleted = false;

        if (currentCount == 0) branch.IsMain = true;

        if (branch.IsMain && currentCount > 0)
        {
            await _branchRepository.UnsetMainBranchAsync(dto.ClinicId);
        }

        var created = await _branchRepository.CreateAsync(branch);
        return _mapper.Map<BranchResponseDto>(created);
    }

    public async Task<BranchResponseDto> UpdateBranchAsync(Guid id, UpdateBranchRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID is required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException($"Branch {id} not found");

        // Logic: If user promotes this branch to Main, ensure we unset the old Main first
        if (dto.IsMain && !branch.IsMain)
        {
            await _branchRepository.UnsetMainBranchAsync(branch.ClinicId);
        }

        _mapper.Map(dto, branch);
        branch.UpdatedAt = DateTime.UtcNow;

        var updated = await _branchRepository.UpdateAsync(branch);
        _logger.LogInformation("Branch {Id} updated", id);

        return _mapper.Map<BranchResponseDto>(updated);
    }

    public async Task DeleteBranchAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID is required", nameof(id));

        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException($"Branch {id} not found");

        // Logic: Prevent deleting the Main branch to ensure the clinic always has a HQ
        if (branch.IsMain)
        {
            throw new InvalidOperationException("Cannot delete the Main Branch. Please assign a new Main Branch first.");
        }

        await _branchRepository.DeleteAsync(branch);
        _logger.LogInformation("Branch {Id} deleted", id);
    }
}