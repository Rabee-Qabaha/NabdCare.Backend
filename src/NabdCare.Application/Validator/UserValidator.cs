using FluentValidation;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Validator;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(user => user.PasswordHash)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(user => user.ClinicId)
            .NotEmpty().WithMessage("ClinicId is required.");

        RuleFor(user => user.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(user => user.Role)
            .NotEmpty().WithMessage("Role is required.")
            .IsInEnum().WithMessage("Invalid role specified.");
    }
}