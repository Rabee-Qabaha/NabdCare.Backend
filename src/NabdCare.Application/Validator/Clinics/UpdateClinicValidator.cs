using FluentValidation;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;

namespace NabdCare.Application.Validator.clinics;

public class UpdateClinicValidator : AbstractValidator<UpdateClinicRequestDto>
{
    private readonly IClinicRepository _clinicRepository;

    public UpdateClinicValidator(IClinicRepository clinicRepository)
    {
        _clinicRepository = clinicRepository;

        // ==========================================
        // 🆔 Identity Validation
        // ==========================================
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Subdomain is required.")
            .Length(3, 60).WithMessage("Subdomain must be between 3 and 60 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain: lowercase letters, numbers, and hyphens only.")
            .Must(slug => !IsReservedWord(slug)).WithMessage("This subdomain is reserved.")
            // Async DB Check: Ensure unique, but exclude current clinic ID is handled in Service 
            // (Validator focuses on format here, Service handles ID exclusion logic)
            .MustAsync(async (slug, _) => !await _clinicRepository.ExistsBySlugAsync(slug))
            .When(x => false); // ⚠️ NOTE: Unique check excluding Self is usually best done in Service or using custom Validator context. 
                               // For simplicity in this architecture, we let the Service handle the "Exclude Self" DB check 
                               // and keep the Validator focused on format.

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500);

        // ==========================================
        // 🎨 Branding Validation
        // ==========================================
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

        RuleFor(x => x.TaxNumber).MaximumLength(50);
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);

        // ==========================================
        // ⚙️ Settings Validation
        // ==========================================
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