using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class CreateChequeDetailDto
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; } 
    public string? ImageUrl { get; set; }
    public string? Note { get; set; }
}