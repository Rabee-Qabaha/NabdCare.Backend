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
        
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.JobTitle).MaximumLength(100);
        RuleFor(x => x.ProfilePictureUrl).MaximumLength(1000);
        RuleFor(x => x.Bio).MaximumLength(500);
        RuleFor(x => x.LicenseNumber).MaximumLength(100);
    }
}