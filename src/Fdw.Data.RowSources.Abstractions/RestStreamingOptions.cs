namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Options for REST pagination-based streaming.
/// </summary>
public sealed class RestStreamingOptions : HttpRowEnumeratorOptions
{
    /// <summary>
    /// Gets or sets the pagination style.
    /// </summary>
    public IRestPaginationStyle PaginationStyle { get; set; } = RestPaginationStyles.OffsetLimit;

    /// <summary>
    /// Gets or sets the query parameter name for offset.
    /// Default is "offset".
    /// </summary>
    public string OffsetParameter { get; set; } = "offset";

    /// <summary>
    /// Gets or sets the query parameter name for limit.
    /// Default is "limit".
    /// </summary>
    public string LimitParameter { get; set; } = "limit";

    /// <summary>
    /// Gets or sets the query parameter name for page number.
    /// Default is "page".
    /// </summary>
    public string PageParameter { get; set; } = "page";

    /// <summary>
    /// Gets or sets the query parameter name for cursor.
    /// Default is "cursor".
    /// </summary>
    public string CursorParameter { get; set; } = "cursor";

    /// <summary>
    /// Gets or sets the JSON path to extract the next cursor from response.
    /// Example: "$.meta.next_cursor"
    /// </summary>
    public string? NextCursorPath { get; set; }

    /// <summary>
    /// Gets or sets whether to parse Link header for pagination.
    /// Default is true.
    /// </summary>
    public bool ParseLinkHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets the JSON path to the total count in response.
    /// Example: "$.meta.total"
    /// </summary>
    public string? TotalCountPath { get; set; }
}