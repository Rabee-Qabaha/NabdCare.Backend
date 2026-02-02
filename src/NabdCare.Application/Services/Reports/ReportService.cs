using NabdCare.Application.DTOs.Reports;
using NabdCare.Application.Interfaces.Reports;

namespace NabdCare.Application.Services.Reports;

public class ReportService : IReportService
{
    private readonly IReportRepository _repository;

    public ReportService(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<StatementDto> GetClinicStatementAsync(Guid clinicId, DateTime start, DateTime end)
    {
        // 1. Get Opening Balance
        decimal openingBalance = await _repository.GetOpeningBalanceAsync(clinicId, start);

        // 2. Get Transactions
        var invoices = await _repository.GetInvoiceTransactionsAsync(clinicId, start, end);
        var payments = await _repository.GetPaymentTransactionsAsync(clinicId, start, end);

        // 3. Merge and Sort
        var allLines = invoices.Concat(payments).OrderBy(x => x.Date).ToList();

        // 4. Calculate Running Balance
        decimal runningBalance = openingBalance;
        foreach (var line in allLines)
        {
            runningBalance += line.Debit - line.Credit;
            line.RunningBalance = runningBalance;
        }

        return new StatementDto
        {
            ClinicId = clinicId,
            StartDate = start,
            EndDate = end,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            Lines = allLines
        };
    }

    public async Task<object> GetPaymentReceiptAsync(Guid paymentId)
    {
        var receiptData = await _repository.GetPaymentReceiptDataAsync(paymentId);
        if (receiptData == null) throw new KeyNotFoundException("Payment not found");
        return receiptData;
    }
}