using FluentValidation;
using NabdCare.Application.DTOs.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Validator.Payments;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequestDto>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Context).IsInEnum();
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Currency).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");

        // Patient context validation
        RuleFor(x => x.PatientId)
            .NotEmpty().When(x => x.Context == PaymentContext.Patient)
            .WithMessage("PatientId is required for patient payments.");

        // Clinic context validation (for SuperAdmins creating payments for other clinics)
        RuleFor(x => x.ClinicId)
            .NotEmpty().When(x => x.Context == PaymentContext.Clinic)
            .WithMessage("ClinicId is required for clinic payments.");

        // Cheque validation
        RuleFor(x => x.ChequeDetail)
            .NotNull().When(x => x.Method == PaymentMethod.Cheque)
            .WithMessage("Cheque details are required for cheque payments.");

        When(x => x.Method == PaymentMethod.Cheque && x.ChequeDetail != null, () =>
        {
            RuleFor(x => x.ChequeDetail!.ChequeNumber).NotEmpty();
            RuleFor(x => x.ChequeDetail!.BankName).NotEmpty();
            RuleFor(x => x.ChequeDetail!.DueDate).GreaterThan(x => x.ChequeDetail!.IssueDate)
                .WithMessage("Due date must be after the issue date.");
            RuleFor(x => x.ChequeDetail!.Amount).GreaterThan(0);
            RuleFor(x => x.ChequeDetail!.Currency).IsInEnum();
        });
    }
}