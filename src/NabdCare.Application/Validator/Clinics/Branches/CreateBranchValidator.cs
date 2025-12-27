using FluentValidation;
using NabdCare.Application.DTOs.Clinics.Branches;

namespace NabdCare.Application.Validator.Clinics.Branches;

public class CreateBranchValidator : AbstractValidator<CreateBranchRequestDto>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}