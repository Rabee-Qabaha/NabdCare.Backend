using NabdCare.Application.DTOs.Reports;

namespace NabdCare.Application.Interfaces.Reports;

public interface IReportService
{
    Task<StatementDto> GetClinicStatementAsync(Guid clinicId, DateTime start, DateTime end);
    Task<object> GetPaymentReceiptAsync(Guid paymentId); // Returns a simple object for now
}