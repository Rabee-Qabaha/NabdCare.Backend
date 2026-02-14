using FluentValidation;
using NabdCare.Application.DTOs.Payments;

namespace NabdCare.Application.Validator.Payments;

public class UpdateChequeDetailValidator : AbstractValidator<UpdateChequeDetailDto>
{
    public UpdateChequeDetailValidator()
    {
        RuleFor(x => x.ChequeNumber).NotEmpty();
        RuleFor(x => x.BankName).NotEmpty();
        RuleFor(x => x.DueDate).GreaterThan(x => x.IssueDate)
            .WithMessage("Due date must be after the issue date.");
    }
}