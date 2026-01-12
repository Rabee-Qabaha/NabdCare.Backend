using FluentValidation;
using NabdCare.Application.DTOs.Roles;

namespace NabdCare.Application.Validator.Roles;

public class CloneRoleRequestValidator : AbstractValidator<CloneRoleRequestDto>
{
    public CloneRoleRequestValidator()
    {
        RuleFor(x => x.NewRoleName)
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.ColorCode)
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .When(x => !string.IsNullOrEmpty(x.ColorCode))
            .WithMessage("Color code must be a valid Hex format (e.g. #FF0000).");
            
        RuleFor(x => x.IconClass)
            .MaximumLength(50).WithMessage("Icon class is too long.");
    }
}