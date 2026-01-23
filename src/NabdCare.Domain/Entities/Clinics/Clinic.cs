using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Entities.Users;    // ✅ Added
using NabdCare.Domain.Entities.Patients; // ✅ Added
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinics;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    // Example: "ramallah-medic" -> ramallah-medic.nabdcare.com
    public string Slug { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // ==========================================
    // 🎨 Branding & Legal
    // ==========================================
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }        
    public string? RegistrationNumber { get; set; }

    // ==========================================
    // ⚙️ Configuration (Mapped via Fluent API in ClinicConfiguration)
    // ==========================================
    public ClinicSettings Settings { get; set; } = new();
    
    public int BranchCount { get; set; } = 1;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    // ==========================================
    // 🔗 Navigation Collections (For Backend Queries)
    // ==========================================
    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    
    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}