using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Base class for lookup endpoints: a search over one domain's names that answers a page of
/// <see cref="LookupItem"/> and nothing else (FDW-1194).
/// </summary>
/// <typeparam name="TDomain">The domain's record, which the lookup is answered from.</typeparam>
/// <remarks>
/// A picker offers a name as the user types and a create form asks whether a name is taken. Neither needs a list's
/// summaries, so a lookup reads the domain table alone, in one query with the search as its filter: no implementation,
/// child or other member is read to find a name. The route is <c>/{resource}/lookup</c>; <c>?q=</c> is compared with the
/// name as <c>?match=</c> says (contains, startsWith or exact), <c>?id=</c> keeps only the members with those identifiers
/// (so a picker holding a stored id can show its name), <c>?total=true</c> adds how many names the search selects in all,
/// and the page is sized and capped by the same settings as every list. A lookup is gated by the resource's own read
/// permission and shows exactly the rows its list shows: a derived lookup states what its list leaves out in
/// <see cref="Includes"/>.
/// </remarks>
public abstract class LookupEndpointBase<TDomain> : CrudPageEndpointBase<TDomain, LookupItem>
    where TDomain : DomainConfiguration, new()
{
    /// <summary>Initializes a new instance of the <see cref="LookupEndpointBase{TDomain}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected LookupEndpointBase(ILogger logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override bool Searchable => true;

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/lookup";

    /// <inheritdoc />
    protected override string EndpointSummary => $"Look up {ResourceName} by name";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        $"Streams one page of {ResourceName} names in name order, found by ?q= compared as ?match= says " +
        "(contains, startsWith or exact). ?total=true adds how many names the search selects in all.";

    /// <summary>Reads one page of the domain's rows for a query.</summary>
    /// <param name="query">The query: the search as its filter, the page, and whether to count.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The page of domain rows, or the failure that ended it.</returns>
    protected abstract Task<IGenericResult<ConfigurationPage<TDomain>>> Read(DomainQuery query, CancellationToken ct);

    /// <summary>Says whether a row the page read is offered, with what the resource's list leaves out.</summary>
    /// <param name="row">The domain row.</param>
    /// <returns>True to offer the row.</returns>
    /// <remarks>
    /// A row left out still counts toward the page's size, exactly as in <see cref="CrudPageEndpointBase{TItem, TSummary}"/>,
    /// and toward a requested total, which counts the rows the search selects. The default offers every row.
    /// </remarks>
    protected virtual bool Includes(TDomain row) => true;

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<TDomain>>> LoadPage(PaginatedListRequest paging, CancellationToken ct)
        => Read(new DomainQuery(Search.Filter(nameof(DomainConfiguration.Name), nameof(DomainConfiguration.Id)), null, paging.Skip, paging.Take, Search.IncludeTotal), ct);

    /// <inheritdoc />
    protected override ValueTask<bool> Include(TDomain item, CancellationToken ct) => ValueTask.FromResult(Includes(item));

    /// <inheritdoc />
    protected override Task<IGenericResult<LookupItem>> ToSummary(TDomain item, CancellationToken ct)
        => Task.FromResult(GenericResult<LookupItem>.Success(new LookupItem
        {
            Id = item.Id,
            Name = item.Name,
            Implementation = item.Implementation,
        }));
}
