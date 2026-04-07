using System.Collections.Generic;

namespace Sellorio.Results;

public class PagedList<TItem>
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required IList<TItem> Items { get; init; }
}
