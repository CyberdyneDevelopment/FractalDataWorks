using System.Collections.Generic;
using System.Linq;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// Cuts one page from a list that was read whole and is searched in memory, in the envelope every list answers in (FDW-1194).
/// </summary>
/// <remarks>
/// For the lists that hang from one member -- a data set's fields, a store's containers -- which are read with their owner
/// and are small beside the domain tables. The page is cut from the members the search selects, in the order given, so a
/// total counts the selected members and not the page; <c>hasMore</c> says whether another page follows.
/// </remarks>
public static class ListWindow
{
    /// <summary>Cuts a page from members that are already selected and ordered.</summary>
    /// <typeparam name="T">The member type.</typeparam>
    /// <param name="selected">The members the search selected, in the order the page follows.</param>
    /// <param name="paging">The page asked for, already sized by the pagination settings.</param>
    /// <param name="includeTotal">Whether the page carries how many members were selected in all.</param>
    /// <returns>The page.</returns>
    public static StreamedPageResponse<T> Of<T>(IReadOnlyList<T> selected, PaginatedListRequest paging, bool includeTotal)
        => new(
            paging.Skip,
            paging.Take,
            selected.Count > paging.Skip + paging.Take,
            selected.Skip(paging.Skip).Take(paging.Take).ToList().ToAsyncEnumerable(),
            () => null,
            includeTotal ? selected.Count : null);
}
