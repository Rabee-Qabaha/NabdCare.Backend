using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums.Invoice;

[ExportTsEnum]
public enum InvoiceItemType
{
    BasePlan = 0,
    AddonUser = 1,
    AddonBranch = 2,
    BonusItem = 3,
    Tax = 4,
    Discount = 5,
    ProrationCredit = 6
}