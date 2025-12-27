using AutoMapper;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Domain.Entities.Subscriptions;

namespace NabdCare.Application.Mappings;

public class SubscriptionProfile : Profile
{
    public SubscriptionProfile()
    {
        // 1. Create Request -> Entity
        CreateMap<CreateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, o => o.Ignore())
            // Map Paid Add-ons
            .ForMember(d => d.PurchasedBranches, o => o.MapFrom(s => s.ExtraBranches))
            .ForMember(d => d.PurchasedUsers, o => o.MapFrom(s => s.ExtraUsers))
            // Map Free Bonuses ✅
            .ForMember(d => d.BonusBranches, o => o.MapFrom(s => s.BonusBranches))
            .ForMember(d => d.BonusUsers, o => o.MapFrom(s => s.BonusUsers))
            // Ignore Computed/System Fields
            .ForMember(d => d.IncludedBranchesSnapshot, o => o.Ignore())
            .ForMember(d => d.IncludedUsersSnapshot, o => o.Ignore())
            .ForMember(d => d.Fee, o => o.Ignore())
            .ForMember(d => d.Type, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.StartDate, o => o.Ignore())
            .ForMember(d => d.EndDate, o => o.Ignore())
            .ForMember(d => d.Clinic, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.PreviousSubscription, o => o.Ignore())
            .ForMember(d => d.PreviousSubscriptionId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());

        // 2. Update Request -> Entity
        CreateMap<UpdateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.PurchasedBranches, o => o.MapFrom(s => s.ExtraBranches))
            .ForMember(d => d.PurchasedUsers, o => o.MapFrom(s => s.ExtraUsers))
            // Map Free Bonuses ✅
            .ForMember(d => d.BonusBranches, o => o.MapFrom(s => s.BonusBranches))
            .ForMember(d => d.BonusUsers, o => o.MapFrom(s => s.BonusUsers))
            // Ignore IDs
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ClinicId, o => o.Ignore())
            // Preserve Snapshots (handled in service logic if plan changes)
            .ForMember(d => d.IncludedBranchesSnapshot, o => o.Ignore())
            .ForMember(d => d.IncludedUsersSnapshot, o => o.Ignore())
            // System
            .ForMember(d => d.Clinic, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.PreviousSubscription, o => o.Ignore())
            .ForMember(d => d.PreviousSubscriptionId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());

        // 3. Entity -> Response
        // AutoMapper maps properties by name matching automatically (including MaxBranches/MaxUsers).
        CreateMap<Subscription, SubscriptionResponseDto>();

        // 4. Entity Cloning (Renewal)
        CreateMap<Subscription, Subscription>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.PreviousSubscriptionId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.PreviousSubscription, o => o.Ignore())
            // Copy Limits ✅
            .ForMember(d => d.IncludedBranchesSnapshot, o => o.MapFrom(s => s.IncludedBranchesSnapshot))
            .ForMember(d => d.PurchasedBranches, o => o.MapFrom(s => s.PurchasedBranches))
            .ForMember(d => d.BonusBranches, o => o.MapFrom(s => s.BonusBranches)) // Copy Bonus
            
            .ForMember(d => d.IncludedUsersSnapshot, o => o.MapFrom(s => s.IncludedUsersSnapshot))
            .ForMember(d => d.PurchasedUsers, o => o.MapFrom(s => s.PurchasedUsers))
            .ForMember(d => d.BonusUsers, o => o.MapFrom(s => s.BonusUsers)) // Copy Bonus

            // Reset Time/Status
            .ForMember(d => d.StartDate, o => o.Ignore())
            .ForMember(d => d.EndDate, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());
    }
}