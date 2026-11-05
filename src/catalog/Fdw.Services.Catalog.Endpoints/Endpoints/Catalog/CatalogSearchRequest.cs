using System.Collections.Generic;
using FastEndpoints;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request containing search criteria for catalog entries.</summary>
public class CatalogSearchRequest
{
    /// <summary>Gets or sets the free-text search query (bound from ?q=). Left out, the search lists everything: a browse.</summary>
    [BindFrom("q")]
    public string? Query { get; set; }

    /// <summary>Gets or sets the kinds to search, each one of <see cref="CatalogKinds"/>. Left empty, every kind is searched.</summary>
    public IList<string> EntityTypes { get; set; } = [];

    /// <summary>Gets or sets how many matches to skip, for the next page.</summary>
    public int? Skip { get; set; }

    /// <summary>Gets or sets how many matches to return, as <c>?take</c>: the DefaultPaginationSize setting when left out, at most MaxPaginationSize.</summary>
    public int? Take { get; set; }
}
