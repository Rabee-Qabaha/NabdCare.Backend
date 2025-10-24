using FluentValidation;
using NabdCare.Application.DTOs.Users;

namespace NabdCare.Application.Validator.Users;

public class UpdateUserRequestDtoValidator : AbstractValidator<UpdateUserRequestDto>
{
    public UpdateUserRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.")
            .NotEqual(Guid.Empty).WithMessage("Invalid role ID.");
    }
}