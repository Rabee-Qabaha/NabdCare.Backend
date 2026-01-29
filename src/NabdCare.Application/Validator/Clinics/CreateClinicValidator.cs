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

        // Identity Validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Subdomain is required.")
            .Length(3, 60).WithMessage("Subdomain must be between 3 and 60 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain: lowercase letters, numbers, and hyphens only.")
            .Must(slug => !IsReservedWord(slug)).WithMessage("This subdomain is reserved.")
            .MustAsync(async (slug, _) => !await _clinicRepository.ExistsBySlugAsync(slug))
            .WithMessage("This subdomain is already taken.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress();

        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MinimumLength(5).WithMessage("Address must be at least 5 characters long.")
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

        // Branding Validation
        RuleFor(x => x.Website)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Invalid Website URL.");

        RuleFor(x => x.LogoUrl)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.LogoUrl))
            .WithMessage("Invalid Logo URL.");

        RuleFor(x => x.TaxNumber).MaximumLength(50);
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);

        // Settings Validation
        RuleFor(x => x.Settings).ChildRules(s => {
            s.RuleFor(x => x.TimeZone).NotEmpty();
            s.RuleFor(x => x.Currency).Length(3);
        }).When(x => x.Settings != null);
    }

    private bool IsReservedWord(string slug) => 
        new[] { "admin", "api", "www", "mail", "clinic", "support", "billing", "auth" }.Contains(slug.ToLower());
}