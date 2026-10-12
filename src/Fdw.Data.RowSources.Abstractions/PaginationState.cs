namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Holds pagination state for REST streaming enumeration.
/// </summary>
internal sealed class PaginationState
{
    /// <summary>
    /// Gets or sets the next URL (for Link header pagination).
    /// </summary>
    public string? NextUrl { get; set; }

    /// <summary>
    /// Gets or sets the cursor token (for cursor-based pagination).
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Gets or sets the current offset (for offset/limit pagination).
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// Gets or sets the current page number (for page number pagination).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of pages processed.
    /// </summary>
    public int PagesProcessed { get; set; }
}
