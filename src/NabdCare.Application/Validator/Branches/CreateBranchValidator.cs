using FluentValidation;
using NabdCare.Application.DTOs.Clinics.Branches;

namespace NabdCare.Application.Validator.Branches;

public class CreateBranchValidator : AbstractValidator<CreateBranchRequestDto>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}