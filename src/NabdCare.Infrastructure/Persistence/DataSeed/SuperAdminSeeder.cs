using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Invoices; // ✅ Added
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class SuperAdminSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<SuperAdminSeeder> _logger;

    public int Order => 4;

    public SuperAdminSeeder(
        NabdCareDbContext dbContext,
        IPasswordService passwordService,
        ILogger<SuperAdminSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding SuperAdmin user and demo clinic...");

        try
        {
            await SeedSuperAdminUserAsync();
            await SeedDemoClinicAsync();
            
            _logger.LogInformation("✅ SuperAdmin and demo clinic seeding completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SuperAdmin seeding failed: {Message}", ex.Message);
            throw;
        }
    }

    private async Task SeedSuperAdminUserAsync()
    {
        // ... (Keep existing SuperAdmin user logic) ...
        var superAdminEmail = "sadmin@nabd.care";
        var exists = await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == superAdminEmail);
        if (exists) return;

        var superAdminRole = await _dbContext.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        if (superAdminRole == null) return;

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = superAdminEmail,
            FullName = "Super Admin",
            RoleId = superAdminRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System:Seeder"
        };
        superAdmin.PasswordHash = _passwordService.HashPassword(superAdmin, "Admin@123!");
        _dbContext.Users.Add(superAdmin);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedDemoClinicAsync()
    {
        var demoClinicEmail = "info@ramallahmedical.ps";
        var clinic = await _dbContext.Clinics.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Email == demoClinicEmail);

        if (clinic == null)
        {
            _logger.LogInformation("🔄 Creating demo clinic 'Ramallah Medical Center'...");
            
            var clinicId = Guid.NewGuid();
            var activeSubId = Guid.NewGuid();
            var activeInvoiceId = Guid.NewGuid();

            // 1. Clinic
            clinic = new Clinic
            {
                Id = clinicId,
                Name = "Ramallah Medical Center",
                Slug = "ramallah-center", 
                Email = demoClinicEmail,
                Phone = "+970-2-2987654",
                Address = "Al-Irsal Street, Downtown Ramallah, Palestine",
                Status = SubscriptionStatus.Active,
                BranchCount = 3,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            };

            // 2. Subscriptions
            var activeSub = new Subscription
            {
                Id = activeSubId,
                ClinicId = clinicId,
                PlanId = "STD_Y", 
                Type = SubscriptionType.Yearly,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1),
                Fee = 1400.00m, 
                Status = SubscriptionStatus.Active,
                IncludedBranchesSnapshot = 1,
                PurchasedBranches = 2, 
                IncludedUsersSnapshot = 2,
                PurchasedUsers = 5,    
                AutoRenew = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            };

            clinic.Subscriptions = new List<Subscription> { activeSub };

            // 3. ✅ SEED INVOICES (Linked to Subscription)
            var activeInvoice = new Invoice
            {
                Id = activeInvoiceId,
                InvoiceNumber = "INV-2025-00001",
                ClinicId = clinicId,
                SubscriptionId = activeSubId,
                BilledToName = "Ramallah Medical Center",
                BilledToAddress = "Al-Irsal Street, Downtown Ramallah, Palestine",
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = InvoiceStatus.Paid, // Mark as paid for demo
                Type = InvoiceType.NewSubscription,
                SubTotal = 1400.00m,
                TotalAmount = 1400.00m,
                PaidAmount = 1400.00m,
                PaidDate = DateTime.UtcNow,
                CreatedBy = "System:Seeder",
                Items = new List<InvoiceItem>
                {
                    new() { Description = "Standard Yearly Plan", Quantity = 1, UnitPrice = 500.00m, Total = 500.00m, Type = InvoiceItemType.BasePlan },
                    new() { Description = "Extra Branches (2)", Quantity = 2, UnitPrice = 200.00m, Total = 400.00m, Type = InvoiceItemType.AddonBranch },
                    new() { Description = "Extra Users (5)", Quantity = 5, UnitPrice = 100.00m, Total = 500.00m, Type = InvoiceItemType.AddonUser }
                }
            };

            _dbContext.Clinics.Add(clinic);
            _dbContext.Invoices.Add(activeInvoice); // Explicitly add invoice

            // 4. Branches
            var branches = new List<Branch>
            {
                new() { Id = Guid.NewGuid(), ClinicId = clinicId, Name = "Ramallah HQ", IsMain = true, CreatedAt = DateTime.UtcNow, CreatedBy = "System:Seeder" },
                new() { Id = Guid.NewGuid(), ClinicId = clinicId, Name = "Nablus Branch", IsMain = false, CreatedAt = DateTime.UtcNow, CreatedBy = "System:Seeder" },
                new() { Id = Guid.NewGuid(), ClinicId = clinicId, Name = "Hebron Branch", IsMain = false, CreatedAt = DateTime.UtcNow, CreatedBy = "System:Seeder" }
            };
            _dbContext.Branches.AddRange(branches);
            
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ Clinic, Subscription, Invoice, and Branches created.");
        }

        // Users
        await SeedClinicUsersAsync(clinic.Id);
    }

    private async Task SeedClinicUsersAsync(Guid clinicId)
    {
        // ... (Keep existing User Seeding logic) ...
        var clinicAdminRole = await _dbContext.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Name == "ClinicAdmin");
        if (clinicAdminRole != null && !await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "cadmin@nabd.care"))
        {
            var user = new User { Id = Guid.NewGuid(), ClinicId = clinicId, RoleId = clinicAdminRole.Id, Email = "cadmin@nabd.care", FullName = "Admin User", IsActive = true, CreatedBy = "System:Seeder" };
            user.PasswordHash = _passwordService.HashPassword(user, "Admin@123!");
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}