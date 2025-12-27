using AutoMapper;
using NabdCare.Application.DTOs.Pagination;

namespace NabdCare.Application.Common;

public static class MappingExtensions
{
    /// <summary>
    /// Extension method to map PaginatedResult<TSource> to PaginatedResult<TDestination>.
    /// </summary>
    public static PaginatedResult<TDestination> ToPaginatedDto<TSource, TDestination>(
        this PaginatedResult<TSource> source, 
        IMapper mapper)
    {
        return new PaginatedResult<TDestination>
        {
            Items = mapper.Map<IEnumerable<TDestination>>(source.Items),
            HasMore = source.HasMore,
            NextCursor = source.NextCursor,
            TotalCount = source.TotalCount
        };
    }
}