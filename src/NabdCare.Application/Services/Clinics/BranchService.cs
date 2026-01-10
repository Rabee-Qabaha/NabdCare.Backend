using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Clinics;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly ILogger<BranchService> _logger;

    public BranchService(
        IBranchRepository branchRepository,
        ISubscriptionRepository subscriptionRepository,
        ITenantContext tenantContext,
        IMapper mapper,
        ILogger<BranchService> logger)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<BranchResponseDto>> GetBranchesAsync(Guid? clinicId = null)
    {
        // 🛡️ SECURITY: Force Clinic Filtering if not SuperAdmin
        if (!_tenantContext.IsSuperAdmin)
        {
            if (_tenantContext.ClinicId == null) return new List<BranchResponseDto>();
            clinicId = _tenantContext.ClinicId.Value;
        }

        var branches = await _branchRepository.GetListAsync(clinicId);
        return _mapper.Map<List<BranchResponseDto>>(branches);
    }

    public async Task<BranchResponseDto?> GetBranchByIdAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) return null;

        // 🛡️ SECURITY: Prevent accessing other clinic's data
        if (!_tenantContext.IsSuperAdmin && branch.ClinicId != _tenantContext.ClinicId)
        {
            _logger.LogWarning("Unauthorized access attempt to Branch {BranchId} by Tenant {TenantId}", id, _tenantContext.ClinicId);
            return null;
        }

        return _mapper.Map<BranchResponseDto>(branch);
    }

    public async Task<BranchResponseDto> CreateBranchAsync(CreateBranchRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        // 🛡️ SECURITY: Enforce Tenant ID
        if (!_tenantContext.IsSuperAdmin && dto.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("Cannot create branches for another clinic.");

        // 1. LIMIT CHECK
        var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(dto.ClinicId);
        if (activeSub == null)
        {
            throw new DomainException(
                "No active subscription found for this clinic.", 
                ErrorCodes.SUBSCRIPTION_REQUIRED
            );
        }

        var currentCount = await _branchRepository.CountByClinicIdAsync(dto.ClinicId);
        if (currentCount >= activeSub.MaxBranches)
        {
            throw new DomainException(
                $"Branch limit reached ({activeSub.MaxBranches}). Upgrade plan to add more.", 
                ErrorCodes.LIMIT_EXCEEDED
            );
        }

        // 2. Map & Defaults
        var branch = _mapper.Map<Branch>(dto);
        branch.Id = Guid.NewGuid();
        branch.CreatedAt = DateTime.UtcNow;
        branch.IsDeleted = false;
        
        // Default to Active upon creation
        branch.IsActive = true; 

        // Auto-Main if it's the first branch
        if (currentCount == 0) branch.IsMain = true;

        // If this new branch claims to be Main, unset the previous Main
        if (branch.IsMain && currentCount > 0)
        {
            await _branchRepository.UnsetMainBranchAsync(dto.ClinicId);
        }

        var created = await _branchRepository.CreateAsync(branch);
        
        _logger.LogInformation("Branch {BranchId} created for Clinic {ClinicId}", created.Id, created.ClinicId);
        return _mapper.Map<BranchResponseDto>(created);
    }

    public async Task<BranchResponseDto> UpdateBranchAsync(Guid id, UpdateBranchRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException($"Branch {id} not found");

        // 🛡️ SECURITY
        if (!_tenantContext.IsSuperAdmin && branch.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("Unauthorized update attempt.");

        // 🔍 LOGIC: Determine intention *before* mapping overwrites the object
        bool isPromotingToMain = dto.IsMain && !branch.IsMain;
        bool isDemotingFromMain = !dto.IsMain && branch.IsMain;

        // 🛑 Rule: Cannot manually uncheck "Main" (must promote another one instead)
        if (isDemotingFromMain)
        {
            throw new DomainException(
                "You cannot unset the Main Branch directly. Promote a different branch to Main instead.",
                ErrorCodes.INVALID_OPERATION,
                "isMain" // Highlights the switch on frontend
            );
        }

        // 📝 Apply Updates
        _mapper.Map(dto, branch);
        branch.UpdatedAt = DateTime.UtcNow;

        // ⚡ Apply Side Effects
        if (isPromotingToMain)
        {
            // Unset old main
            await _branchRepository.UnsetMainBranchAsync(branch.ClinicId);
            
            // Force this one to be Active (In case DTO tried to set it to Inactive)
            // A Main Branch MUST be active.
            branch.IsActive = true; 
        }

        var updated = await _branchRepository.UpdateAsync(branch);
        
        _logger.LogInformation("Branch {BranchId} updated. Main Status: {IsMain}", id, branch.IsMain);
        return _mapper.Map<BranchResponseDto>(updated);
    }

    public async Task<BranchResponseDto> ToggleBranchStatusAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException($"Branch {id} not found");
        
        // 🛡️ SECURITY
        if (!_tenantContext.IsSuperAdmin && branch.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("Unauthorized access.");

        // 🛑 SAFETY: Prevent Deactivating the Main Branch
        if (branch.IsMain && branch.IsActive)
        {
             throw new DomainException(
                 "Cannot deactivate the Main Branch. Please set another branch as Main first.",
                 ErrorCodes.INVALID_OPERATION
             );
        }
        
        branch.IsActive = !branch.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;
        
        var updated = await _branchRepository.UpdateAsync(branch);
        
        _logger.LogInformation("Branch {BranchId} status toggled to {Status}", id, branch.IsActive);
        return _mapper.Map<BranchResponseDto>(updated);
    }

    public async Task DeleteBranchAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new KeyNotFoundException($"Branch {id} not found");

        // 🛡️ SECURITY
        if (!_tenantContext.IsSuperAdmin && branch.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("Unauthorized deletion attempt.");

        // 🛑 SAFETY: Prevent Deleting Main Branch
        if (branch.IsMain)
        {
            throw new DomainException(
                "Cannot delete the Main Branch. Assign a new Main Branch first.",
                ErrorCodes.INVALID_OPERATION
            );
        }

        await _branchRepository.DeleteAsync(branch);
        _logger.LogInformation("Branch {BranchId} deleted", id);
    }
}