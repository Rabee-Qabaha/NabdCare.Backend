using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions; // ✅ Added
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Application.Interfaces.Permissions; // ✅ Added
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Clinics;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly ILogger<BranchService> _logger;
    private readonly IPermissionEvaluator _permissionEvaluator; // ✅ Deep Security

    public BranchService(
        IBranchRepository branchRepository,
        ISubscriptionRepository subscriptionRepository,
        ITenantContext tenantContext,
        IMapper mapper,
        ILogger<BranchService> logger,
        IPermissionEvaluator permissionEvaluator)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    public async Task<List<BranchResponseDto>> GetBranchesAsync(Guid? clinicId = null)
    {
        // 1. Resolve Scope
        Guid targetClinicId;

        if (_tenantContext.IsSuperAdmin)
        {
            // SuperAdmin can view specific clinic or empty list if null
            if (!clinicId.HasValue) return new List<BranchResponseDto>();
            targetClinicId = clinicId.Value;
        }
        else
        {
            // Tenant must view own clinic
            targetClinicId = _tenantContext.ClinicId!.Value;

            // 🔐 Permission Check
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.View))
                throw new UnauthorizedAccessException("You do not have permission to view branches.");
        }

        var branches = await _branchRepository.GetListAsync(targetClinicId);
        return _mapper.Map<List<BranchResponseDto>>(branches);
    }

    public async Task<BranchResponseDto?> GetBranchByIdAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) return null;

        // 🔐 Security: Check Ownership
        if (!CanAccessBranch(branch))
            throw new UnauthorizedAccessException("You do not have permission to view this branch.");

        // 🔐 Permission Check
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.View))
             throw new UnauthorizedAccessException("You lack permissions to view branch details.");

        return _mapper.Map<BranchResponseDto>(branch);
    }

    public async Task<BranchResponseDto> CreateBranchAsync(CreateBranchRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        // 🔐 Security: Enforce Tenant Scope
        if (!_tenantContext.IsSuperAdmin)
        {
            if (dto.ClinicId != _tenantContext.ClinicId)
                throw new UnauthorizedAccessException("Cannot create branches for another clinic.");
            
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.Create))
                throw new UnauthorizedAccessException("You lack permissions to create branches.");
        }

        // 1. SUBSCRIPTION LIMIT CHECK
        var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(dto.ClinicId);
        if (activeSub == null)
        {
            throw new DomainException(
                "No active subscription found. Subscribe to a plan to add branches.", 
                ErrorCodes.SUBSCRIPTION_REQUIRED
            );
        }

        var currentCount = await _branchRepository.CountByClinicIdAsync(dto.ClinicId);
        
        // MaxBranches handles the sum logic (Included + Purchased + Bonus)
        if (currentCount >= activeSub.MaxBranches)
        {
            _logger.LogWarning("Branch limit reached for Clinic {ClinicId}: {Count}/{Max}", dto.ClinicId, currentCount, activeSub.MaxBranches);
            throw new DomainException(
                $"Branch limit reached ({activeSub.MaxBranches}). Upgrade your plan or buy addons to create more.", 
                ErrorCodes.LIMIT_EXCEEDED
            );
        }

        // 2. Logic & Defaults
        var branch = _mapper.Map<Branch>(dto);
        branch.Id = Guid.NewGuid();
        branch.CreatedAt = DateTime.UtcNow;
        branch.IsDeleted = false;
        branch.IsActive = true; 

        // Auto-Main if first branch
        if (currentCount == 0) branch.IsMain = true;

        // Logic: If new branch is Main, unset previous
        if (branch.IsMain && currentCount > 0)
        {
            // Security check for setting main
            if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.SetMain))
                 throw new UnauthorizedAccessException("You lack permissions to set the Main Branch.");

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
        if (branch == null) throw new DomainException($"Branch {id} not found.", ErrorCodes.NOT_FOUND);

        // 🔐 Security
        if (!CanAccessBranch(branch))
            throw new UnauthorizedAccessException("Access denied.");

        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.Edit))
             throw new UnauthorizedAccessException("You lack permissions to edit branches.");

        // 🔍 Business Logic
        bool isPromotingToMain = dto.IsMain && !branch.IsMain;
        bool isDemotingFromMain = !dto.IsMain && branch.IsMain;

        if (isDemotingFromMain)
        {
            throw new DomainException(
                "You cannot unset the Main Branch directly. Promote a different branch instead.",
                ErrorCodes.INVALID_OPERATION
            );
        }

        if (isPromotingToMain)
        {
            if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.SetMain))
                 throw new UnauthorizedAccessException("You lack permissions to change the Main Branch.");

            await _branchRepository.UnsetMainBranchAsync(branch.ClinicId);
            branch.IsActive = true; // Main must be active
        }

        _mapper.Map(dto, branch);
        branch.UpdatedAt = DateTime.UtcNow;

        var updated = await _branchRepository.UpdateAsync(branch);
        return _mapper.Map<BranchResponseDto>(updated);
    }

    public async Task<BranchResponseDto> ToggleBranchStatusAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new DomainException($"Branch {id} not found.", ErrorCodes.NOT_FOUND);
        
        // 🔐 Security
        if (!CanAccessBranch(branch)) throw new UnauthorizedAccessException("Access denied.");

        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.ToggleStatus))
             throw new UnauthorizedAccessException("You lack permissions to activate/deactivate branches.");

        // 🛑 Rule: Cannot deactivate Main Branch
        if (branch.IsMain && branch.IsActive)
        {
             throw new DomainException(
                 "Cannot deactivate the Main Branch. Set another branch as Main first.",
                 ErrorCodes.INVALID_OPERATION
             );
        }
        
        branch.IsActive = !branch.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;
        
        var updated = await _branchRepository.UpdateAsync(branch);
        return _mapper.Map<BranchResponseDto>(updated);
    }

    public async Task DeleteBranchAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null) throw new DomainException($"Branch {id} not found.", ErrorCodes.NOT_FOUND);

        // 🔐 Security
        if (!CanAccessBranch(branch)) throw new UnauthorizedAccessException("Access denied.");

        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Branches.Delete))
             throw new UnauthorizedAccessException("You lack permissions to delete branches.");

        // 🛑 Rule: Cannot delete Main Branch
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

    // ============================================
    // HELPER
    // ============================================
    private bool CanAccessBranch(Branch branch)
    {
        if (_tenantContext.IsSuperAdmin) return true;
        return branch.ClinicId == _tenantContext.ClinicId;
    }
}