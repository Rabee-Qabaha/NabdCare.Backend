using FluentValidation;
using NabdCare.Application.DTOs.Clinics;

namespace NabdCare.Application.Validator.clinics;

public class UpdateClinicValidator : AbstractValidator<UpdateClinicRequestDto>
{
    public UpdateClinicValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(15).When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.SubscriptionStartDate)
            .LessThan(x => x.SubscriptionEndDate)
            .WithMessage("Subscription start date must be before end date.");

        RuleFor(x => x.SubscriptionFee)
            .GreaterThanOrEqualTo(0).WithMessage("Subscription fee must be non-negative.");
        
        RuleFor(x => x.SubscriptionType)
            .IsInEnum().WithMessage("Invalid subscription type specified.");

        RuleFor(x => x.BranchCount)
            .GreaterThan(0).WithMessage("Branch count must be greater than zero.");
    }
}