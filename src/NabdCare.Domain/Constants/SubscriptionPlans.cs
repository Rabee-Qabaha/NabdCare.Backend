using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Constants;

public static class SubscriptionPlans
{
    // 1. TRIAL
    public static readonly PlanDefinition Trial = new(
        Id: "TRIAL", 
        Name: "14-Day Free Trial", 
        Type: SubscriptionType.Monthly, 
        BaseFee: 0m, 
        
        BranchPrice: 0m, 
        IncludedBranches: 1, 
        IncludedUsers: 3,
        UserPrice: 0m,   
        
        DurationDays: 14, 
        AllowAddons: false
    );

    // 2. STANDARD MONTHLY
    public static readonly PlanDefinition StandardMonthly = new(
        Id: "STD_M", 
        Name: "Standard Monthly", 
        Type: SubscriptionType.Monthly, 
        BaseFee: 35.00m, 
        
        BranchPrice: 20.00m, 
        IncludedBranches: 1,
        IncludedUsers: 4,
        UserPrice: 10.00m,   
        
        DurationDays: 30,
        AllowAddons: true
    );

    // 3. STANDARD YEARLY
    public static readonly PlanDefinition StandardYearly = new(
        Id: "STD_Y", 
        Name: "Standard Yearly", 
        Type: SubscriptionType.Yearly, 
        BaseFee: 300.00m, 
        
        BranchPrice: 150.00m, 
        IncludedBranches: 1,
        IncludedUsers: 5,
        UserPrice: 100.00m,   
        
        DurationDays: 365,
        AllowAddons: true
    );
    
    public static readonly IReadOnlyList<PlanDefinition> All = new[] { Trial, StandardMonthly, StandardYearly };
    public static PlanDefinition? GetById(string id) => All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}