using FluentValidation;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;

namespace NabdCare.Application.Validator.clinics;

public class CreateClinicValidator : AbstractValidator<CreateClinicRequestDto>
{
    private readonly IClinicRepository _clinicRepository;

    public CreateClinicValidator(IClinicRepository clinicRepository)
    {
        _clinicRepository = clinicRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(255);

        // ✅ Slug (Subdomain) Validation
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Subdomain is required.")
            .Length(3, 60).WithMessage("Subdomain must be between 3 and 60 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens.")
            .Must(slug => !IsReservedWord(slug)).WithMessage("This subdomain is reserved and cannot be used.")
            // Async DB Check: Ensure it doesn't exist yet
            .MustAsync(async (slug, cancellation) => 
                !await _clinicRepository.ExistsBySlugAsync(slug))
            .WithMessage("This subdomain is already taken.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100);

        // ✅ Phone is now REQUIRED
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        // ✅ Address is now REQUIRED
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

        // ✅ Validate URLs
        RuleFor(x => x.Website)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Website must be a valid URL (e.g., https://example.com).")
            .MaximumLength(255);

        RuleFor(x => x.LogoUrl)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.LogoUrl))
            .WithMessage("Logo URL must be a valid URL.")
            .MaximumLength(500);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).WithMessage("Tax number cannot exceed 50 characters.");

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(50).WithMessage("Registration number cannot exceed 50 characters.");

        // Subscription Validation
        RuleFor(x => x.SubscriptionStartDate)
            .LessThan(x => x.SubscriptionEndDate)
            .WithMessage("Subscription start date must be before end date.");

        RuleFor(x => x.SubscriptionFee)
            .GreaterThanOrEqualTo(0).WithMessage("Subscription fee must be non-negative.");
        
        RuleFor(x => x.SubscriptionType)
            .IsInEnum().WithMessage("Invalid subscription type specified.");

        RuleFor(x => x.BranchCount)
            .GreaterThan(0).WithMessage("Branch count must be greater than zero.");
            
        // ✅ Validate Settings Object
        RuleFor(x => x.Settings).ChildRules(settings => {
            settings.RuleFor(s => s.Currency)
                .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g. USD).")
                .When(s => !string.IsNullOrEmpty(s.Currency));
                
            settings.RuleFor(s => s.TimeZone).NotEmpty().WithMessage("Timezone is required.");
        }).When(x => x.Settings != null);
    }

    private bool IsReservedWord(string slug)
    {
        var reserved = new[] { 
            "www", "api", "admin", "superadmin", "auth", "mail", "dashboard", 
            "support", "billing", "status", "portal", "help", "clinic" 
        };
        return reserved.Contains(slug.ToLower());
    }
}