using FluentValidation;
using NabdCare.Application.DTOs.Clinics.Subscriptions;

namespace NabdCare.Application.Validator.Clinics.Subscriptions;
public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("ClinicId is required.");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("StartDate must be before EndDate.");

        RuleFor(x => x.Fee)
            .GreaterThan(0).WithMessage("Fee must be greater than zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid subscription type.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid subscription status.");
        
        RuleFor(x => x.GracePeriodDays)
            .GreaterThanOrEqualTo(0).LessThanOrEqualTo(90).WithMessage("GracePeriodDays must be greater than or equal to 90.");
    }
}
