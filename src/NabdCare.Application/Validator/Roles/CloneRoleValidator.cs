using FluentValidation;
using NabdCare.Application.DTOs.Roles;

namespace NabdCare.Application.Validator.Roles;

public class CloneRoleValidator : AbstractValidator<CloneRoleRequestDto>
{
    public CloneRoleValidator()
    {
        RuleFor(x => x.NewRoleName)
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\(\)]+$") 
            .When(x => !string.IsNullOrWhiteSpace(x.NewRoleName))
            .WithMessage("Role name can only contain letters, numbers, spaces, hyphens, underscores, and parentheses");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}