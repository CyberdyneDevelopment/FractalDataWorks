using System;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// Internal search record for datasets.
/// </summary>
public class SearchableDataSetRecord
{
    /// <summary>Gets or sets the dataset identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the dataset name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the dataset description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the associated connection name.</summary>
    public string? ConnectionName { get; set; }
}