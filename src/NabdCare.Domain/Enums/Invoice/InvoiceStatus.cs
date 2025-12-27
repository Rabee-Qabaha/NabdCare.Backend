using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums.Invoice;

[ExportTsEnum]
public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,     // Sent to customer, awaiting payment
    Paid = 2,       // Fully paid
    PartiallyPaid = 3,
    Overdue = 4,    // Past DueDate
    Void = 5,       // Cancelled
    Uncollectible = 6
}