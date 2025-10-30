namespace NabdCare.Application.DTOs.Pagination;

/// <summary>
/// Generic cursor-based pagination result for scalable SaaS APIs.
/// </summary>
public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
}