using NabdCare.Application.DTOs.Invoices;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Application.Interfaces.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByIdForUpdateAsync(Guid id);

    Task<Invoice?> GetByNumberAsync(string invoiceNumber);

    Task<PaginatedResult<Invoice>> GetPagedAsync(
        InvoiceListRequestDto filter, 
        Func<IQueryable<Invoice>, IQueryable<Invoice>>? abacFilter = null);

    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateAsync(Invoice invoice);
    
    Task<string> GenerateNextInvoiceNumberAsync();
}