using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums.Invoice;

[ExportTsEnum]
public enum InvoiceType
{
    NewSubscription = 0,
    Renewal = 1,
    Upgrade = 2,    // Prorated add-ons
    Adjustment = 3  // Manual correction
}