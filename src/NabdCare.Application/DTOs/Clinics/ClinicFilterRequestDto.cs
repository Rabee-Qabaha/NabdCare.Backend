using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ClinicFilterRequestDto : PaginationRequestDto
{
    // Global Search (Name, Email, Phone, Slug)
    public string? Search { get; set; } 

    // Specific Filters
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    public SubscriptionStatus? Status { get; set; }
    public SubscriptionType? SubscriptionType { get; set; }
    
    // "MinFee" logic: Show clinics paying at least this amount
    public decimal? SubscriptionFee { get; set; } 
    
    // Date Filtering (Exact date or starting from)
    public DateTime? CreatedAt { get; set; }
    
    public bool IncludeDeleted { get; set; } = false;
}