using FluentValidation;
using NabdCare.Application.DTOs.Roles;

namespace NabdCare.Application.Validator.Roles;

public class UpdateRoleValidator : AbstractValidator<UpdateRoleRequestDto>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Role name can only contain letters, numbers, spaces, hyphens, and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");

        RuleFor(x => x.ColorCode)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorCode))
            .WithMessage("ColorCode must be a valid hex color (e.g., #3B82F6)");

        RuleFor(x => x.IconClass)
            .MaximumLength(50).WithMessage("IconClass cannot exceed 50 characters")
            .Matches(@"^[a-z0-9\-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.IconClass))
            .WithMessage("IconClass must contain only lowercase letters, numbers, and hyphens");
    }
}