using System;
using System.Collections.Generic;
using FastEndpoints;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request containing search criteria for catalog entries.</summary>
public class CatalogSearchRequest
{
    /// <summary>Gets or sets the free-text search query (bound from ?q=).</summary>
    [BindFrom("q")]
    public string? Query { get; set; }

    /// <summary>Gets or sets the entity types to filter by.</summary>
    public IList<string> EntityTypes { get; set; } = [];

    /// <summary>Gets or sets the tags to filter by.</summary>
    public IList<string> Tags { get; set; } = [];
}