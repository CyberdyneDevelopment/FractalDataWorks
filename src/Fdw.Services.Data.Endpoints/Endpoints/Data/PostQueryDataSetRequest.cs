using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for the POST DataSet query endpoint.
/// Filters are supplied in the request body, enabling complex multi-field queries
/// without query-string length limits and allowing richer filter expressions in future.
/// </summary>
public sealed class PostQueryDataSetRequest
{
    /// <summary>Gets or sets the DataSet name (path parameter).</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of rows to skip (default 0).</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the maximum number of rows to return (default 50, clamped to 1–1000).</summary>
    public int Take { get; set; } = 50;

    /// <summary>
    /// Gets or sets field equality filters as field-name → value pairs.
    /// All supplied conditions are ANDed together.
    /// </summary>
    public IReadOnlyDictionary<string, string> Filters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
