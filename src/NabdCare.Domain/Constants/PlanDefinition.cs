using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Constants;

/// <summary>
/// Represents the pricing and rules for a subscription plan.
/// This is a Value Object in the Domain Layer.
/// </summary>
[ExportTsInterface]
public record PlanDefinition(
    string Id, 
    string Name, 
    SubscriptionType Type, 
    decimal BaseFee,       
    
    // 🌳 Branch Pricing
    decimal BranchPrice,   
    int IncludedBranches,  
    
    // 👤 User Pricing
    decimal UserPrice,
    int IncludedUsers,

    int DurationDays,
    bool AllowAddons
);