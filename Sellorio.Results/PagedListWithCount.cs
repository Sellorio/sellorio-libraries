using System.Collections.Generic;

namespace Sellorio.Results;

public class PagedListWithCount<TItem>
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public required int TotalPages { get; init; }
    public required List<TItem> Items { get; init; }
}
