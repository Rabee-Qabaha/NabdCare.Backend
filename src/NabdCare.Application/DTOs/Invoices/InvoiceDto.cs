using NabdCare.Application.DTOs.Payments;
using NabdCare.Domain.Enums.Invoice;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Invoices;

[ExportTsClass]
public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public Guid SubscriptionId { get; set; }

    // Snapshot Data
    public string BilledToName { get; set; } = string.Empty;
    public string? BilledToAddress { get; set; }
    public string? BilledToTaxNumber { get; set; }

    // Dates & Status
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public InvoiceType Type { get; set; }

    // Deliverables
    public string? PdfUrl { get; set; }
    public string? HostedPaymentUrl { get; set; }

    // Financials with Currency
    public string Currency { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
    
    // Payment History (Allocations)
    public List<PaymentAllocationDto> Payments { get; set; } = new();
}