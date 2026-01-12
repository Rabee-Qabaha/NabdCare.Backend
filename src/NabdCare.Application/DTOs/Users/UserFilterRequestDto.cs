using NabdCare.Application.DTOs.Pagination;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class UserFilterRequestDto: PaginationRequestDto
{
    public string? Search { get; set; }
    public bool IncludeDeleted { get; set; } = false;
    
    // 🔍 New Filters
    public Guid? ClinicId { get; set; }
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
    
    // 📅 Date Range
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}