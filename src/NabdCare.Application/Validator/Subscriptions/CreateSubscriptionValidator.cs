using FluentValidation;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Domain.Constants;

namespace NabdCare.Application.Validator.Subscriptions;

public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("ClinicId is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan ID is required.")
            .Must(id => SubscriptionPlans.GetById(id) != null)
            .WithMessage("Invalid Plan ID. Please choose a valid subscription plan.");

        RuleFor(x => x.ExtraBranches)
            .GreaterThanOrEqualTo(0).WithMessage("Extra branches cannot be negative.");

        RuleFor(x => x.ExtraUsers)
            .GreaterThanOrEqualTo(0).WithMessage("Extra users cannot be negative.");
    }
}