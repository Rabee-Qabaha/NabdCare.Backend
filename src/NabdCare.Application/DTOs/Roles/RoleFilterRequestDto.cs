using NabdCare.Application.DTOs.Pagination;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Roles;

[ExportTsClass]
public class RoleFilterRequestDto : PaginationRequestDto
{
    [TsOptional]
    public string? Search { get; set; }
    
    public bool IncludeDeleted { get; set; } = false;

    [TsOptional]
    public Guid? ClinicId { get; set; } 

    [TsOptional]
    public bool? IsSystemRole { get; set; }

    [TsOptional]
    public bool? IsTemplate { get; set; }

    // Date Range
    [TsOptional]
    public DateTime? FromDate { get; set; }
    [TsOptional]
    public DateTime? ToDate { get; set; }
    
    [TsOptional]
    public string? RoleOrigin { get; set; } // "System", "Template", "Custom", "All"
}