using NabdCare.Application.DTOs.Invoices;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Application.Interfaces.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByNumberAsync(string invoiceNumber);

    Task<PaginatedResult<Invoice>> GetPagedAsync(
        InvoiceListRequestDto filter, 
        Func<IQueryable<Invoice>, IQueryable<Invoice>>? abacFilter = null);

    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateAsync(Invoice invoice);
    
    /// <summary>
    /// Generates the next sequential invoice number (e.g., INV-2025-0042)
    /// </summary>
    Task<string> GenerateNextInvoiceNumberAsync();
}