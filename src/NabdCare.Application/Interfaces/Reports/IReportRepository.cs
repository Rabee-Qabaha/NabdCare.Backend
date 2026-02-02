using NabdCare.Application.DTOs.Reports;

namespace NabdCare.Application.Interfaces.Reports;

public interface IReportRepository
{
    Task<decimal> GetOpeningBalanceAsync(Guid clinicId, DateTime start);
    Task<List<StatementLineItemDto>> GetInvoiceTransactionsAsync(Guid clinicId, DateTime start, DateTime end);
    Task<List<StatementLineItemDto>> GetPaymentTransactionsAsync(Guid clinicId, DateTime start, DateTime end);
    Task<dynamic?> GetPaymentReceiptDataAsync(Guid paymentId);
}