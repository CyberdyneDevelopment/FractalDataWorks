using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>One page of catalog search matches, with how many matched in all.</summary>
public sealed class CatalogSearchResponse
{
    /// <summary>Gets or sets the page of matches, the closest names first.</summary>
    public IList<CatalogEntryDto> Items { get; set; } = [];

    /// <summary>Gets or sets how many matches were skipped.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets how many matches the page was asked to hold.</summary>
    public int Take { get; set; }

    /// <summary>Gets or sets how many entries matched in all, before paging.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets a value indicating whether more matches follow this page.</summary>
    public bool HasMore { get; set; }

    /// <summary>Gets or sets the kinds that were searched.</summary>
    public IList<string> Kinds { get; set; } = [];
}
