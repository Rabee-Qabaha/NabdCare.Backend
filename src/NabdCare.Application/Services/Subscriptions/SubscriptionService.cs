using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
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
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository repository,
        IClinicRepository clinicRepository,
        IInvoiceService invoiceService,
        IUserContext userContext,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _repository = repository;
        _clinicRepository = clinicRepository;
        _invoiceService = invoiceService;
        _userContext = userContext;
        _mapper = mapper;
        _logger = logger;
    }

    // ============================================
    // 1. CREATION
    // ============================================
    public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto)
    {
        var clinic = await _clinicRepository.GetEntityByIdAsync(dto.ClinicId);
        if (clinic == null) throw new KeyNotFoundException($"Clinic {dto.ClinicId} not found.");

        var plan = SubscriptionPlans.GetById(dto.PlanId);
        if (plan == null) throw new ArgumentException($"Invalid Plan ID: {dto.PlanId}");

        decimal totalFee = plan.BaseFee 
                         + (dto.ExtraBranches * plan.BranchPrice) 
                         + (dto.ExtraUsers * plan.UserPrice);

        var startDate = dto.CustomStartDate ?? DateTime.UtcNow;
        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            ClinicId = dto.ClinicId,
            PlanId = plan.Id,
            Type = plan.Type,
            StartDate = startDate,
            EndDate = startDate.AddDays(plan.DurationDays),
            BillingCycleAnchor = startDate,
            
            // ✅ Fix: Use requested currency or default to USD
            Currency = dto.Currency ?? "USD",
            
            Fee = totalFee,
            Status = plan.Id == "TRIAL" ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
            
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

        await _repository.CreateAsync(sub);

        clinic.Status = sub.Status;
        clinic.BranchCount = sub.MaxBranches;
        await _clinicRepository.UpdateAsync(clinic);

        if (sub.Fee > 0)
        {
            await _invoiceService.GenerateInvoiceAsync(new GenerateInvoiceRequestDto
            {
                ClinicId = sub.ClinicId,
                SubscriptionId = sub.Id,
                Type = InvoiceType.NewSubscription,
                
                // ✅ Fix: Pass the Subscription's Currency to the Invoice
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

        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    // ============================================
    // 2. UPDATE
    // ============================================
    public async Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto)
    {
        var sub = await _repository.GetByIdAsync(id);
        if (sub == null) return null;

        var plan = SubscriptionPlans.GetById(sub.PlanId);
        if (plan == null) throw new InvalidOperationException("Plan definition not found.");
        var now = DateTime.UtcNow;

        // Proration Logic
        if (sub.Status == SubscriptionStatus.Active && sub.EndDate > now)
        {
            var items = new List<GenerateInvoiceItemDto>();
            var daysRemaining = (sub.EndDate - now).Days;

            if (daysRemaining > 0 && (dto.ExtraUsers > sub.PurchasedUsers || dto.ExtraBranches > sub.PurchasedBranches))
            {
                if (dto.ExtraUsers > sub.PurchasedUsers)
                {
                    int diff = dto.ExtraUsers - sub.PurchasedUsers;
                    decimal rate = sub.Type == SubscriptionType.Monthly ? plan.UserPrice / 30m : plan.UserPrice / 365m;
                    items.Add(new() { Description = "Capacity Upgrade: Users", Quantity = diff, UnitPrice = Math.Round(rate * daysRemaining, 2), Type = InvoiceItemType.AddonUser });
                }
                if (dto.ExtraBranches > sub.PurchasedBranches)
                {
                    int diff = dto.ExtraBranches - sub.PurchasedBranches;
                    decimal rate = sub.Type == SubscriptionType.Monthly ? plan.BranchPrice / 30m : plan.BranchPrice / 365m;
                    items.Add(new() { Description = "Capacity Upgrade: Branches", Quantity = diff, UnitPrice = Math.Round(rate * daysRemaining, 2), Type = InvoiceItemType.AddonBranch });
                }

                if (items.Any())
                {
                    await _invoiceService.GenerateInvoiceAsync(new GenerateInvoiceRequestDto
                    {
                        ClinicId = sub.ClinicId,
                        SubscriptionId = sub.Id,
                        Type = InvoiceType.Upgrade,
                        
                        // ✅ Fix: Pass Currency to Proration Invoice
                        Currency = sub.Currency,
                        
                        DueDate = now,
                        Items = items
                    });
                }
            }
        }

        // Apply Updates
        sub.PurchasedBranches = dto.ExtraBranches;
        sub.PurchasedUsers = dto.ExtraUsers;
        sub.BonusBranches = dto.BonusBranches;
        sub.BonusUsers = dto.BonusUsers;
        sub.AutoRenew = dto.AutoRenew;
        sub.GracePeriodDays = dto.GracePeriodDays;
        
        if (dto.CancelAtPeriodEnd.HasValue) sub.CancelAtPeriodEnd = dto.CancelAtPeriodEnd.Value;
        if (dto.Status.HasValue) sub.Status = dto.Status.Value;
        if (dto.EndDate.HasValue) sub.EndDate = dto.EndDate.Value;
        
        // Recalculate Recurring Fee
        sub.Fee = plan.BaseFee + (dto.ExtraBranches * plan.BranchPrice) + (dto.ExtraUsers * plan.UserPrice);
        sub.UpdatedAt = now;
        sub.UpdatedBy = _userContext.GetCurrentUserId();

        await _repository.UpdateAsync(sub);

        if (sub.Status == SubscriptionStatus.Active)
        {
            var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
            if (clinic != null)
            {
                clinic.BranchCount = sub.MaxBranches;
                await _clinicRepository.UpdateAsync(clinic);
            }
        }

        return _mapper.Map<SubscriptionResponseDto>(sub);
    }

    // ============================================
    // 3. RENEWAL
    // ============================================
    public async Task<SubscriptionResponseDto> RenewSubscriptionAsync(Guid oldSubscriptionId, SubscriptionType type)
    {
        var oldSub = await _repository.GetByIdAsync(oldSubscriptionId);
        if (oldSub == null) throw new KeyNotFoundException();
        if (await _repository.HasFutureSubscriptionAsync(oldSub.ClinicId, oldSub.EndDate))
            throw new InvalidOperationException("Renewal already queued.");

        var startDate = oldSub.EndDate < DateTime.UtcNow ? DateTime.UtcNow : oldSub.EndDate;
        var newSub = await ExecuteRenewalAsync(oldSub, type, startDate, false);

        if (oldSub.Status != SubscriptionStatus.Expired)
        {
            oldSub.Status = SubscriptionStatus.Expired;
            await _repository.UpdateStatusAsync(oldSub);
        }

        return _mapper.Map<SubscriptionResponseDto>(newSub);
    }

    private async Task<Subscription> ExecuteRenewalAsync(Subscription old, SubscriptionType type, DateTime start, bool isAuto)
    {
        var plan = SubscriptionPlans.GetById(old.PlanId)!;
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
            
            // ✅ Fix: Copy currency from old subscription
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

        await _repository.CreateAsync(sub);

        await _invoiceService.GenerateInvoiceAsync(new GenerateInvoiceRequestDto
        {
            ClinicId = sub.ClinicId,
            SubscriptionId = sub.Id,
            Type = InvoiceType.Renewal,
            
            // ✅ Fix: Ensure renewal invoice matches subscription currency
            Currency = sub.Currency,
            
            DueDate = start.AddDays(7),
            Items = new List<GenerateInvoiceItemDto>
            {
                new() { Description = $"{plan.Name} Renewal", Quantity = 1, UnitPrice = plan.BaseFee, Type = InvoiceItemType.BasePlan },
                new() { Description = "Branches", Quantity = sub.PurchasedBranches, UnitPrice = plan.BranchPrice, Type = InvoiceItemType.AddonBranch },
                new() { Description = "Users", Quantity = sub.PurchasedUsers, UnitPrice = plan.UserPrice, Type = InvoiceItemType.AddonUser }
            }.Where(i => i.Quantity > 0 || i.Type == InvoiceItemType.BasePlan).ToList()
        });

        return sub;
    }

    // ============================================
    // 4. LIFECYCLE / JOBS
    // ============================================
    public async Task<int> ProcessAutoRenewalsAsync(DateTime nowUtc)
    {
        var candidates = await _repository.GetAutoRenewCandidatesAsync(nowUtc);
        int count = 0;
        foreach (var old in candidates)
        {
            if (await _repository.HasFutureSubscriptionAsync(old.ClinicId, old.EndDate)) continue;
            try {
                await ExecuteRenewalAsync(old, old.Type, old.EndDate, true);
                old.Status = SubscriptionStatus.Expired;
                await _repository.UpdateStatusAsync(old);
                count++;
            } catch (Exception ex) { _logger.LogError(ex, "Auto-renew failed for {Id}", old.Id); }
        }
        return count;
    }

    public async Task<int> ProcessScheduledCancellationsAsync(DateTime nowUtc)
    {
        var candidates = await _repository.GetCancellationCandidatesAsync(nowUtc);
        int count = 0;
        foreach (var sub in candidates)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            sub.CanceledAt = nowUtc;
            await _repository.UpdateAsync(sub);
            
            var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
            if (clinic != null) {
                clinic.Status = SubscriptionStatus.Cancelled;
                await _clinicRepository.UpdateAsync(clinic);
            }
            count++;
        }
        return count;
    }

    public async Task<int> ProcessExpirationsAsync(DateTime nowUtc)
    {
        var expired = await _repository.GetExpiredCandidatesAsync(nowUtc);
        int count = 0;
        foreach (var sub in expired)
        {
            bool hasFuture = await _repository.HasFutureSubscriptionAsync(sub.ClinicId, sub.EndDate);
            sub.Status = SubscriptionStatus.Expired;
            await _repository.UpdateStatusAsync(sub);

            if (!hasFuture) {
                var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
                if (clinic != null && clinic.Status == SubscriptionStatus.Active) {
                    clinic.Status = SubscriptionStatus.Expired;
                    await _clinicRepository.UpdateAsync(clinic);
                }
            }
            count++;
        }
        return count;
    }

    public async Task<int> ActivateFutureSubscriptionsAsync(DateTime nowUtc)
    {
        var subs = await _repository.GetFutureSubscriptionsStartingByAsync(nowUtc);
        int count = 0;
        foreach (var sub in subs)
        {
            sub.Status = SubscriptionStatus.Active;
            if (sub.PreviousSubscriptionId.HasValue) {
                var prev = await _repository.GetByIdAsync(sub.PreviousSubscriptionId.Value);
                if (prev != null) { prev.Status = SubscriptionStatus.Expired; await _repository.UpdateStatusAsync(prev); }
            }
            await _repository.UpdateStatusAsync(sub);
            
            var clinic = await _clinicRepository.GetEntityByIdAsync(sub.ClinicId);
            if (clinic != null) {
                clinic.Status = SubscriptionStatus.Active;
                clinic.BranchCount = sub.MaxBranches;
                await _clinicRepository.UpdateAsync(clinic);
            }
            count++;
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
        sub.AutoRenew = enable;
        await _repository.UpdateAsync(sub);
        return _mapper.Map<SubscriptionResponseDto>(sub);
    }
    
    public async Task<bool> DeleteSubscriptionAsync(Guid id) => await _repository.DeleteAsync(id);

    // ============================================
    // 6. QUERIES
    // ============================================
    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments)
        => _mapper.Map<SubscriptionResponseDto>(await _repository.GetByIdAsync(id, includePayments, true));
    
    public async Task<SubscriptionResponseDto?> GetActiveSubscriptionAsync(Guid clinicId)
        => _mapper.Map<SubscriptionResponseDto>(await _repository.GetActiveByClinicIdAsync(clinicId));

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto p, bool i, Func<IQueryable<Subscription>, IQueryable<Subscription>>? f)
        => MapPaged(await _repository.GetByClinicIdPagedAsync(clinicId, p, i, f));

    public async Task<PaginatedResult<SubscriptionResponseDto>> GetAllPagedAsync(PaginationRequestDto p, bool i, Func<IQueryable<Subscription>, IQueryable<Subscription>>? f)
        => MapPaged(await _repository.GetAllPagedAsync(p, i, f));
        
    public async Task<PaginatedResult<SubscriptionResponseDto>> GetPagedAsync(PaginationRequestDto p, bool i, Func<IQueryable<Subscription>, IQueryable<Subscription>>? f)
        => MapPaged(await _repository.GetPagedAsync(p, i, f));

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
}