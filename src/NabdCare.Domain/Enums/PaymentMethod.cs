using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum PaymentMethod
{
    Cash,
    Cheque,
    Visa,
    BankTransfer
}