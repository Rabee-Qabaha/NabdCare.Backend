using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Pagination;

/// <summary>
/// Generic cursor-based pagination result for scalable SaaS APIs.
/// </summary>
[ExportTsClass]
public class PaginatedResult<T>
{
    /// <summary>
    /// The list of paginated items.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// Cursor for the next page (if available).
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Indicates whether more items exist beyond this page.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Total number of matching items (not just the page).
    /// </summary>
    public int TotalCount { get; set; }
}