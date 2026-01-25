using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Application.DTOs.Invoices;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Invoices;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Domain.Constants;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;

namespace NabdCare.Application.Services.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository repository,
        IClinicRepository clinicRepository,
        IInvoiceService invoiceService,
        IUserContext userContext,
        ITenantContext tenantContext,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clinicRepository = clinicRepository ?? throw new ArgumentNullException(nameof(clinicRepository));
        _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ============================================
    // 1. CREATION (Transactional)
    // ============================================
    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto)
    {
        // 🔐 Security: Enforce Tenant Scope
        if (!_tenantContext.IsSuperAdmin)
        {
            // If not SuperAdmin, can only create for own clinic (Self-Service)
            // Note: Endpoints usually force this, but we enforce it here too.
            if (dto.ClinicId != _tenantContext.ClinicId)
            {
                _logger.LogWarning("User {UserId} tried to create subscription for another clinic {TargetClinic}", _userContext.GetCurrentUserId(), dto.ClinicId);
                throw new UnauthorizedAccessException("You can only purchase subscriptions for your own clinic.");
            }
        }

        // 1. Validation & Setup
        var clinic = await _clinicRepository.GetEntityByIdAsync(dto.ClinicId);
        if (clinic == null) 
            throw new DomainException($"Clinic {dto.ClinicId} not found.", ErrorCodes.CLINIC_NOT_FOUND);

        var plan = SubscriptionPlans.GetById(dto.PlanId);
        if (plan == null) 
            throw new DomainException($"Invalid Plan ID: {dto.PlanId}", ErrorCodes.INVALID_ARGUMENT, "PlanId");

        // 2. Logic Calculation
        decimal totalFee = plan.BaseFee
                           + (dto.ExtraBranches * plan.BranchPrice)
                           + (dto.ExtraUsers * plan.UserPrice);

        var startDate = dto.CustomStartDate ?? DateTime.UtcNow;

        // 3. Prepare Entities (In Memory)
        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            ClinicId = dto.ClinicId,
            PlanId = plan.Id,
            Type = plan.Type,
            StartDate = startDate,
            EndDate = startDate.AddDays(plan.DurationDays),
            BillingCycleAnchor = startDate,
            Currency = dto.Currency ?? "USD",
            Fee = totalFee,
            Status = plan.Id == "TRIAL" ? SubscriptionStatus.Trial : SubscriptionStatus.Active,

            // Snapshots & Limits
            IncludedBranchesSnapshot = plan.IncludedBranches,
            PurchasedBranches = dto.ExtraBranches,
            BonusBranches = dto.BonusBranches,
            IncludedUsersSnapshot = plan.IncludedUsers,
            PurchasedUsers = dto.ExtraUsers,
            BonusUsers = dto.BonusUsers,

            AutoRenew = dto.AutoRenew && plan.Id != "TRIAL",
            GracePeriodDays = plan.Id == "TRIAL" ? 3 : 7,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userContext.GetCurrentUserId() ?? "System"
        };

        // 4. Execution with Transaction
        using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            // A. Create Subscription
            await _repository.CreateAsync(sub);

            // B. Update Clinic State
            clinic.Status = sub.Status;
            clinic.BranchCount = sub.MaxBranches;
            await _clinicRepository.UpdateAsync(clinic);

            // C. Generate Invoice
            if (sub.Fee > 0)
            {
                await _invoiceService.GenerateInvoiceAsync(new GenerateInvoiceRequestDto
                {
                    ClinicId = sub.ClinicId,
                    SubscriptionId = sub.Id,
                    Type = InvoiceType.NewSubscription,
                    Currency = sub.Currency,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    Items = new List<GenerateInvoiceItemDto>
                    {
                        new() { Description = $"{plan.Name} Subscription", Quantity = 1, UnitPrice = plan.BaseFee, Type = InvoiceItemType.BasePlan },
                        new() { Description = "Branch Addons", Quantity = dto.ExtraBranches, UnitPrice = plan.BranchPrice, Type = InvoiceItemType.AddonBranch },
                        new() { Description = "User Addons", Quantity = dto.ExtraUsers, UnitPrice = plan.UserPrice, Type = InvoiceItemType.AddonUser }
                    }.Where(i => i.Quantity > 0 || i.Type == InvoiceItemType.BasePlan).ToList()
                });
            }

            // D. Commit
            await transaction.CommitAsync();
            _logger.LogInformation("Created Subscription {Id} for Clinic {ClinicId}", sub.Id, clinic.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create subscription for Clinic {ClinicId}", clinic.Id);
            throw new DomainException("Failed to create subscription.", ErrorCodes.OPERATION_FAILED);
        }

        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    // ============================================
    // 2. UPDATE (Transactional)
    // ============================================
    public async Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto)
    {
        var sub = await _repository.GetByIdAsync(id);
        if (sub == null) return null;

        // 🔐 Security Check
        if (!CanAccessSubscription(sub))
            throw new UnauthorizedAccessException("You do not have permission to modify this subscription.");

        var plan = SubscriptionPlans.GetById(sub.PlanId);
        if (plan == null) 
            throw new DomainException("Plan definition not found for this subscription.", ErrorCodes.INVALID_OPERATION);
        
        var now = DateTime.UtcNow;

        using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            // 1. Proration Logic (Revenue Capture)
            if (sub.Status == SubscriptionStatus.Active && sub.EndDate > now)
            {
                var items = new List<GenerateInvoiceItemDto>();
                var daysRemaining = (sub.EndDate - now).Days;

                if (daysRemaining > 0)
                {
                    if (dto.ExtraUsers > sub.PurchasedUsers)
                    {
                        int diff = dto.ExtraUsers - sub.PurchasedUsers;
                        decimal dailyRate = sub.Type == SubscriptionType.Monthly ? plan.UserPrice / 30m : plan.UserPrice / 365m;
                        items.Add(new() { Description = "Capacity Upgrade: Users", Quantity = diff, UnitPrice = Math.Round(dailyRate * daysRemaining, 2), Type = InvoiceItemType.AddonUser });
                    }

                    if (dto.ExtraBranches > sub.PurchasedBranches)
                    {
                        int diff = dto.ExtraBranches - sub.PurchasedBranches;
                        decimal dailyRate = sub.Type == SubscriptionType.Monthly ? plan.BranchPrice / 30m : plan.BranchPrice / 365m;
                        items.Add(new() { Description = "Capacity Upgrade: Branches", Quantity = diff, UnitPrice = Math.Round(dailyRate * daysRemaining, 2), Type = InvoiceItemType.AddonBranch });
                    }

                    if (items.Any())
                    {
                        await _invoiceService.GenerateInvoiceAsync(new GenerateInvoiceRequestDto
                        {
                            ClinicId = sub.ClinicId,
                            SubscriptionId = sub.Id,
                            Type = InvoiceType.Upgrade,
                            Currency = sub.Currency,
                            DueDate = now,
                            Items = items
                        });
                    }
                }
            }

            // 2. Apply Updates
            sub.PurchasedBranches = dto.ExtraBranches;
            sub.PurchasedUsers = dto.ExtraUsers;
            sub.BonusBranches = dto.BonusBranches;
            sub.BonusUsers = dto.BonusUsers;
            sub.AutoRenew = dto.AutoRenew;
            sub.GracePeriodDays = dto.GracePeriodDays;

            if (dto.CancelAtPeriodEnd.HasValue) sub.CancelAtPeriodEnd = dto.CancelAtPeriodEnd.Value;
            if (dto.Status.HasValue) sub.Status = dto.Status.Value;
            if (dto.EndDate.HasValue) sub.EndDate = dto.EndDate.Value;

            sub.Fee = plan.BaseFee + (dto.ExtraBranches * plan.BranchPrice) + (dto.ExtraUsers * plan.UserPrice);
            sub.UpdatedAt = now;
            sub.UpdatedBy = _userContext.GetCurrentUserId();

            await _repository.UpdateAsync(sub);

            // 3. Sync Clinic Entity
            if (sub.Status == SubscriptionStatus.Active)
            {
                var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
                if (clinic != null)
                {
                    clinic.BranchCount = sub.MaxBranches;
                    await _clinicRepository.UpdateAsync(clinic);
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Updated Subscription {Id}", sub.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to update subscription {Id}", id);
            throw new DomainException("Failed to update subscription.", ErrorCodes.OPERATION_FAILED);
        }

        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    // ============================================
    // 3. RENEWAL
    // ============================================
    public async Task<SubscriptionResponseDto> RenewSubscriptionAsync(Guid oldSubscriptionId, SubscriptionType type)
    {
        var oldSub = await _repository.GetByIdAsync(oldSubscriptionId);
        if (oldSub == null) 
            throw new DomainException("Original subscription not found.", ErrorCodes.NOT_FOUND);
        
        // 🔐 Security Check
        if (!CanAccessSubscription(oldSub))
            throw new UnauthorizedAccessException("You do not have permission to renew this subscription.");

        if (await _repository.HasFutureSubscriptionAsync(oldSub.ClinicId, oldSub.EndDate))
            throw new DomainException("A renewal is already queued for this subscription.", ErrorCodes.CONFLICT);

        var startDate = oldSub.EndDate < DateTime.UtcNow ? DateTime.UtcNow : oldSub.EndDate;
        
        using var transaction = await _repository.BeginTransactionAsync();
        try 
        {
            // Transaction handled here now
            var newSub = await ExecuteRenewalAsync(oldSub, type, startDate, false);

            if (oldSub.Status != SubscriptionStatus.Expired)
            {
                oldSub.Status = SubscriptionStatus.Expired;
                await _repository.UpdateStatusAsync(oldSub); // Now atomic with renewal creation
            }

            await transaction.CommitAsync();
            return _mapper.Map<SubscriptionResponseDto>(newSub);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================================
    // 4. LIFECYCLE / JOBS (Per-Item Transactions)
    // ============================================
    // NOTE: Background jobs (System) generally bypass strict Tenant Checks since they run as System.
    
    public async Task<int> ProcessAutoRenewalsAsync(DateTime nowUtc)
    {
        var candidates = await _repository.GetAutoRenewCandidatesAsync(nowUtc);
        int count = 0;
        foreach (var old in candidates)
        {
            if (await _repository.HasFutureSubscriptionAsync(old.ClinicId, old.EndDate)) continue;
            
            using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                // 1. Create Renewal
                await ExecuteRenewalAsync(old, old.Type, old.EndDate, true);
                
                // 2. Expire Old (Now Atomic)
                old.Status = SubscriptionStatus.Expired;
                await _repository.UpdateStatusAsync(old);
                
                await transaction.CommitAsync();
                count++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Auto-renew failed for {Id}", old.Id);
                // Continue to next candidate
            }
        }
        return count;
    }

    public async Task<int> ProcessScheduledCancellationsAsync(DateTime nowUtc)
    {
        var candidates = await _repository.GetCancellationCandidatesAsync(nowUtc);
        int count = 0;
        foreach (var sub in candidates)
        {
            using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.CanceledAt = nowUtc;
                await _repository.UpdateAsync(sub);

                var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
                if (clinic != null)
                {
                    clinic.Status = SubscriptionStatus.Cancelled;
                    await _clinicRepository.UpdateAsync(clinic);
                }

                await transaction.CommitAsync();
                count++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Cancellation Job failed for {Id}", sub.Id);
            }
        }
        return count;
    }

    public async Task<int> ProcessExpirationsAsync(DateTime nowUtc)
    {
        var expired = await _repository.GetExpiredCandidatesAsync(nowUtc);
        int count = 0;
        foreach (var sub in expired)
        {
            using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                bool hasFuture = await _repository.HasFutureSubscriptionAsync(sub.ClinicId, sub.EndDate);
                
                sub.Status = SubscriptionStatus.Expired;
                await _repository.UpdateStatusAsync(sub);

                // If no future subscription takes over, expire the clinic too
                if (!hasFuture)
                {
                    var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
                    if (clinic != null && clinic.Status == SubscriptionStatus.Active)
                    {
                        clinic.Status = SubscriptionStatus.Expired;
                        await _clinicRepository.UpdateAsync(clinic);
                    }
                }
                
                await transaction.CommitAsync();
                count++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Expiration Job failed for {Id}", sub.Id);
            }
        }
        return count;
    }

    public async Task<int> ActivateFutureSubscriptionsAsync(DateTime nowUtc)
    {
        var subs = await _repository.GetFutureSubscriptionsStartingByAsync(nowUtc);
        int count = 0;
        foreach (var sub in subs)
        {
            using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                // 1. Activate New
                sub.Status = SubscriptionStatus.Active;
                await _repository.UpdateStatusAsync(sub);

                // 2. Expire Old
                if (sub.PreviousSubscriptionId.HasValue)
                {
                    var prev = await _repository.GetByIdAsync(sub.PreviousSubscriptionId.Value);
                    if (prev != null)
                    {
                        prev.Status = SubscriptionStatus.Expired;
                        await _repository.UpdateStatusAsync(prev);
                    }
                }

                // 3. Sync Clinic
                var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
                if (clinic != null)
                {
                    clinic.Status = SubscriptionStatus.Active;
                    clinic.BranchCount = sub.MaxBranches;
                    await _clinicRepository.UpdateAsync(clinic);
                }

                await transaction.CommitAsync();
                count++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Future Activation Job failed for {Id}", sub.Id);
            }
        }
        return count;
    }

    // ============================================
    // 5. HELPER ACTIONS
    // ============================================
    public async Task<bool> CancelSubscriptionAsync(Guid id)
    {
        var sub = await _repository.GetByIdAsync(id);
        if (sub == null) return false;

        // 🔐 Security Check
        if (!CanAccessSubscription(sub))
            throw new UnauthorizedAccessException("You do not have permission to cancel this subscription.");

        sub.CancelAtPeriodEnd = true;
        sub.AutoRenew = false;
        sub.CancellationReason = "User requested cancellation";
        sub.UpdatedAt = DateTime.UtcNow;
        sub.UpdatedBy = _userContext.GetCurrentUserId();

        await _repository.UpdateAsync(sub);
        return true;
    }

    public async Task<SubscriptionResponseDto?> UpdateSubscriptionStatusAsync(Guid id, SubscriptionStatus status)
    {
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can manually change status.");

        var sub = await _repository.GetByIdAsync(id);
        if (sub == null) return null;
        
        sub.Status = status;
        await _repository.UpdateStatusAsync(sub);
        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    public async Task<SubscriptionResponseDto?> ToggleAutoRenewAsync(Guid id, bool enable)
    {
        var sub = await _repository.GetByIdAsync(id);
        if (sub == null) return null;

        // 🔐 Security Check
        if (!CanAccessSubscription(sub))
            throw new UnauthorizedAccessException("Permission denied.");

        sub.AutoRenew = enable;
        await _repository.UpdateAsync(sub);
        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id)
    {
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete subscriptions.");

        return await _repository.DeleteAsync(id);
    }

    // ============================================
    // 6. QUERIES
    // ============================================
    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments)
    {
        var sub = await _repository.GetByIdAsync(id, includePayments, true);
        if (sub == null) return null;

        // 🔐 Security Check
        if (!CanAccessSubscription(sub))
            throw new UnauthorizedAccessException("You do not have permission to view this subscription.");

        var dto = _mapper.Map<SubscriptionResponseDto>(sub);
        PopulateLatestInvoiceData(sub, dto);
        
        return dto;
    }

    public async Task<SubscriptionResponseDto?> GetActiveSubscriptionAsync(Guid clinicId)
    {
        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
            throw new UnauthorizedAccessException("Access denied.");

        // Repository now includes Invoices (after the fix in Step 1)
        var sub = await _repository.GetActiveByClinicIdAsync(clinicId);
        if (sub == null) return null; 

        var dto = _mapper.Map<SubscriptionResponseDto>(sub);

        PopulateLatestInvoiceData(sub, dto);

        return dto;
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto p, bool i, Func<IQueryable<Subscription>, IQueryable<Subscription>>? f)
    {
        // 🔐 Security Check
        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
            throw new UnauthorizedAccessException("Access denied.");

        return MapPaged(await _repository.GetByClinicIdPagedAsync(clinicId, p, i, f));
    }

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetAllPagedAsync(PaginationRequestDto p, bool i, Func<IQueryable<Subscription>, IQueryable<Subscription>>? f)
    {
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can view all subscriptions.");

        return MapPaged(await _repository.GetAllPagedAsync(p, i, f));
    }

    // Note: GetPagedAsync (generic) should probably be protected or removed if not used by SuperAdmin only logic
    public async Task<PaginatedResult<SubscriptionResponseDto>> GetPagedAsync(PaginationRequestDto p, bool i, Func<IQueryable<Subscription>, IQueryable<Subscription>>? f)
    {
        if (!_tenantContext.IsSuperAdmin)
             throw new UnauthorizedAccessException("Access denied.");
             
        return MapPaged(await _repository.GetPagedAsync(p, i, f));
    }

    private PaginatedResult<SubscriptionResponseDto> MapPaged(PaginatedResult<Subscription> res)
    {
        return new PaginatedResult<SubscriptionResponseDto>
        {
            Items = _mapper.Map<IEnumerable<SubscriptionResponseDto>>(res.Items),
            TotalCount = res.TotalCount,
            HasMore = res.HasMore,
            NextCursor = res.NextCursor
        };
    }

    // ============================================
    // 7. PRIVATE HELPERS (Transactional & Security)
    // ============================================
    
    private bool CanAccessSubscription(Subscription sub)
    {
        if (_tenantContext.IsSuperAdmin) return true;
        return sub.ClinicId == _tenantContext.ClinicId;
    }

    private async Task<Subscription> ExecuteRenewalAsync(Subscription old, SubscriptionType type, DateTime start, bool isAuto)
    {
        var plan = SubscriptionPlans.GetById(old.PlanId) 
                   ?? throw new DomainException($"Plan {old.PlanId} definition not found for renewal.", ErrorCodes.INVALID_OPERATION);
        
        var end = type == SubscriptionType.Monthly ? start.AddMonths(1) : start.AddYears(1);

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            ClinicId = old.ClinicId,
            PlanId = old.PlanId,
            Type = type,
            StartDate = start,
            EndDate = end,
            BillingCycleAnchor = old.BillingCycleAnchor ?? start,
            Currency = old.Currency,
            Fee = plan.BaseFee + (old.PurchasedBranches * plan.BranchPrice) + (old.PurchasedUsers * plan.UserPrice),
            Status = start > DateTime.UtcNow ? SubscriptionStatus.Future : SubscriptionStatus.Active,
            IncludedBranchesSnapshot = old.IncludedBranchesSnapshot,
            PurchasedBranches = old.PurchasedBranches,
            BonusBranches = old.BonusBranches,
            IncludedUsersSnapshot = old.IncludedUsersSnapshot,
            PurchasedUsers = old.PurchasedUsers,
            BonusUsers = old.BonusUsers,
            AutoRenew = true,
            GracePeriodDays = old.GracePeriodDays,
            PreviousSubscriptionId = old.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = isAuto ? "System" : _userContext.GetCurrentUserId() ?? "System"
        };

        // REMOVED INTERNAL TRANSACTION to better support Atomicity in callers
        // using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            // 1. Create Subscription
            await _repository.CreateAsync(sub);

            // 2. Generate Invoice
            await _invoiceService.GenerateInvoiceAsync(new GenerateInvoiceRequestDto
            {
                ClinicId = sub.ClinicId,
                SubscriptionId = sub.Id,
                Type = InvoiceType.Renewal,
                Currency = sub.Currency,
                DueDate = start.AddDays(7),
                Items = new List<GenerateInvoiceItemDto>
                {
                    new() { Description = $"{plan.Name} Renewal", Quantity = 1, UnitPrice = plan.BaseFee, Type = InvoiceItemType.BasePlan },
                    new() { Description = "Branches", Quantity = sub.PurchasedBranches, UnitPrice = plan.BranchPrice, Type = InvoiceItemType.AddonBranch },
                    new() { Description = "Users", Quantity = sub.PurchasedUsers, UnitPrice = plan.UserPrice, Type = InvoiceItemType.AddonUser }
                }.Where(i => i.Quantity > 0 || i.Type == InvoiceItemType.BasePlan).ToList()
            });

            // 3. Commit - MOVED TO CALLER
            // await transaction.CommitAsync();
            _logger.LogInformation("Renewed Subscription {OldId} -> {NewId}", old.Id, sub.Id);
        }
        catch (Exception ex)
        {
            // await transaction.RollbackAsync(); // MOVED TO CALLER
            _logger.LogError(ex, "Renewal failed for {OldId}", old.Id);
            throw new DomainException("Subscription renewal failed.", ErrorCodes.OPERATION_FAILED);
        }

        return sub;
    }
    
    private void PopulateLatestInvoiceData(Subscription sub, SubscriptionResponseDto dto)
    {
        if (sub.Invoices != null && sub.Invoices.Any())
        {
            // Find the most recent invoice based on IssueDate or CreatedAt
            var latest = sub.Invoices
                .OrderByDescending(x => x.IssueDate) // Or x.CreatedAt
                .FirstOrDefault();

            if (latest != null)
            {
                dto.LatestInvoiceId = latest.Id;
                dto.LatestInvoiceNumber = latest.InvoiceNumber;
                dto.LatestInvoiceStatus = latest.Status;
            }
        }
    }
}