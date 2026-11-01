using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Options for HTTP row enumeration with pagination support.
/// </summary>
public class HttpRowEnumeratorOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the page size for pagination.
    /// Default is 100.
    /// </summary>
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of pages to fetch.
    /// 0 means unlimited.
    /// </summary>
    public int MaxPages { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets JSON options for parsing responses.
    /// </summary>
    public JsonRowSourceOptions JsonOptions { get; set; } = new();
}