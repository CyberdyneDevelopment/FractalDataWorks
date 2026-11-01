using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads one name-ordered page out of several domain sources that each answer a page of the same query (FDW-2392).
/// </summary>
/// <remarks>
/// A page of the merge is not a page of any one source, so each source is asked for the first <c>skip + take + 1</c> rows of the query,
/// which is all the merge can need from it: the rows before the window and the row that says more follows. The rows are merged by name,
/// the window is cut, and the page says more follows when the merge holds a row past the window. A source that holds more than it
/// answered always leaves such a row, because it answered <c>skip + take + 1</c> of them. The total is the sum of the sources' totals, and
/// is absent unless every source gave one. Names are compared ignoring case, which is how the sources order them.
/// </remarks>
internal static class MergedDomainPages
{
    /// <summary>One source: reads a page of a query.</summary>
    /// <param name="query">The query, with a skip of zero and a take wide enough for the merge.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal delegate Task<IGenericResult<Rows>> Source(DomainQuery query, CancellationToken cancellationToken);

    /// <summary>The rows one source answered.</summary>
    /// <param name="Items">The rows, in name order.</param>
    /// <param name="Total">How many rows the source selects in all, when the query asked.</param>
    internal sealed record Rows(IReadOnlyList<DomainConfiguration> Items, int? Total);

    /// <summary>Reads a source's page into rows.</summary>
    /// <typeparam name="T">The domain row the source answers.</typeparam>
    /// <param name="page">The page the source answered.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows, or the failure the page ended with.</returns>
    internal static async Task<IGenericResult<Rows>> Collect<T>(ConfigurationPage<T> page, CancellationToken cancellationToken)
        where T : DomainConfiguration
    {
        var items = new List<DomainConfiguration>();
        await foreach (var row in page.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
            items.Add(row);

        return page.Outcome.IsFailure
            ? page.Outcome.ToNewResult<Rows>()
            : GenericResult<Rows>.Success(new Rows(items, page.Total));
    }

    /// <summary>Reads a page of the merge.</summary>
    /// <param name="sources">The sources to merge; one or more.</param>
    /// <param name="query">The caller's query: its filter, its window and whether to count.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page, or the first failure of a source.</returns>
    internal static async Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(
        IReadOnlyList<Source> sources, DomainQuery query, CancellationToken cancellationToken)
    {
        if (sources.Count == 0)
            throw new ArgumentException("A merge needs at least one source.", nameof(sources));

        var wide = new DomainQuery(query.Filter, query.Ordering, 0, query.Skip + query.Take + 1, query.IncludeTotal);
        var merged = new List<DomainConfiguration>();
        int? total = 0;
        foreach (var source in sources)
        {
            var rows = await source(wide, cancellationToken).ConfigureAwait(false);
            if (rows.IsFailure)
                return rows.ToNewResult<ConfigurationPage<DomainConfiguration>>();

            merged.AddRange(rows.Value!.Items);
            total = total is { } sum && rows.Value.Total is { } counted ? sum + counted : null;
        }

        merged.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        var window = merged.Skip(query.Skip).Take(query.Take).ToList();
        return GenericResult<ConfigurationPage<DomainConfiguration>>.Success(new ConfigurationPage<DomainConfiguration>(
            query.Skip, query.Take, merged.Count > query.Skip + query.Take, _ => window.ToAsyncEnumerable(), query.IncludeTotal ? total : null));
    }
}
