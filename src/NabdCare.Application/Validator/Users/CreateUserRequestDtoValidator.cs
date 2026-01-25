using FluentValidation;
using NabdCare.Application.DTOs.Users;

namespace NabdCare.Application.Validator.Users;

public class CreateUserRequestDtoValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(9).WithMessage("Password must be at least 9 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s\-'.]+$")
            .WithMessage("Full name can only contain letters, spaces, hyphens, apostrophes, and periods.");

        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.ClinicId)
            .NotEqual(Guid.Empty).When(x => x.ClinicId.HasValue)
            .WithMessage("Invalid clinic ID.");

        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.JobTitle).MaximumLength(100);
        RuleFor(x => x.ProfilePictureUrl).MaximumLength(1000);
        RuleFor(x => x.Bio).MaximumLength(500);
        RuleFor(x => x.LicenseNumber).MaximumLength(100);
    }
}