using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Pagination;

[ExportTsClass]
public class PaginationRequestDto
{
    /// <summary>
    /// Maximum number of items to retrieve. Default = 20.
    /// </summary>
    /// <summary>
    /// Maximum number of items to retrieve. Default = 20.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Optional cursor (ID of the last record from the previous page).
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Optional field name to sort by (default handled in repository).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Whether to sort in descending order.
    /// </summary>
    public bool? Descending { get; set; }

    /// <summary>
    /// Optional keyword for filtering/search.
    /// </summary>
    public string? Filter { get; set; }
}