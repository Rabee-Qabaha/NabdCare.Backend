using FluentValidation;
using NabdCare.Application.DTOs.Subscriptions;

namespace NabdCare.Application.Validator.Subscriptions;

public class UpdateSubscriptionValidator : AbstractValidator<UpdateSubscriptionRequestDto>
{
    public UpdateSubscriptionValidator()
    {
        RuleFor(x => x.EndDate)
            .Must(date => date == null || date > DateTime.UtcNow.AddDays(-30)) 
            .WithMessage("EndDate cannot be too far in the past.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("Invalid subscription status.");
        
        RuleFor(x => x.ExtraBranches)
            .GreaterThanOrEqualTo(0).WithMessage("Extra branches cannot be negative.");

        RuleFor(x => x.ExtraUsers)
            .GreaterThanOrEqualTo(0).WithMessage("Extra users cannot be negative.");
        
        RuleFor(x => x.GracePeriodDays)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(90)
            .WithMessage("GracePeriodDays must be between 0 and 90.");
    }
}