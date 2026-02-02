using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Reports;

[ExportTsClass]
public class StatementDto
{
    public Guid ClinicId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    
    public List<StatementLineItemDto> Lines { get; set; } = new();
}

[ExportTsClass]
public class StatementLineItemDto
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // "Invoice" or "Payment"
    public string Reference { get; set; } = string.Empty; // Invoice Number or Payment ID
    public string Description { get; set; } = string.Empty;
    
    public decimal Debit { get; set; } // Invoice Amount
    public decimal Credit { get; set; } // Payment Amount
    public decimal RunningBalance { get; set; }
}