using NabdCare.Application.DTOs.Invoices;
using NabdCare.Application.DTOs.Pagination;

namespace NabdCare.Application.Interfaces.Invoices;

public interface IInvoiceService
{
    // Called by System (SubscriptionService)
    Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequestDto request);

    // Called by API/Frontend
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<PaginatedResult<InvoiceDto>> GetPagedAsync(InvoiceListRequestDto request);
    
    Task<InvoiceDto?> VoidInvoiceAsync(Guid id);
    
    Task<InvoiceDto?> WriteOffInvoiceAsync(Guid id, string reason);
}