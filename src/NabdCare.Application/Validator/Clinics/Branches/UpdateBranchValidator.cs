using FluentValidation;
using NabdCare.Application.DTOs.Clinics.Branches;

namespace NabdCare.Application.Validator.Clinics.Branches;

public class UpdateBranchValidator : AbstractValidator<UpdateBranchRequestDto>
{
    public UpdateBranchValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(100).WithMessage("Branch name must not exceed 100 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number too long.");
            
        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Address too long.");
    }
}