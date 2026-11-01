namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Options for OData $skip/$top pagination.
/// </summary>
public sealed class ODataStreamingOptions : HttpRowEnumeratorOptions
{
    /// <summary>
    /// Gets or sets whether to request total count ($count=true).
    /// Default is true.
    /// </summary>
    public bool RequestCount { get; set; } = true;

    /// <summary>
    /// Gets or sets the $select fields.
    /// </summary>
    public string? Select { get; set; }

    /// <summary>
    /// Gets or sets the $filter expression.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the $orderby expression.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Gets or sets the $expand expression.
    /// </summary>
    public string? Expand { get; set; }
}