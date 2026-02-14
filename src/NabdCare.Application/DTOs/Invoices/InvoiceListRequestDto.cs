using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Invoices;

[ExportTsClass]
public class InvoiceListRequestDto : PaginationRequestDto
{
    public Guid? ClinicId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? InvoiceNumber { get; set; }
    public InvoiceStatus? Status { get; set; }
    public InvoiceType? Type { get; set; }
    public Currency? Currency { get; set; }
    
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}