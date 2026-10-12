using System.ComponentModel.DataAnnotations;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// Base class for streaming requests that process large datasets.
/// Provides parameters for controlling streaming behavior and data flow.
/// </summary>
public class StreamingRequest
{
    /// <summary>
    /// Gets or sets the batch size for streaming operations.
    /// </summary>
    [Range(1, 10000, ErrorMessage = "BatchSize must be between 1 and 10000")]
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of items to stream.
    /// </summary>
    [Range(1, 1000000, ErrorMessage = "MaxItems must be between 1 and 1000000")]
    public int? MaxItems { get; set; }

    /// <summary>
    /// Gets or sets a search/filter term for the stream.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the sort field for the stream.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort direction (asc/desc).
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Gets or sets the output format for streaming (json, csv, xml).
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    /// Gets a value indicating whether the sort direction is descending.
    /// </summary>
    public bool IsDescending => string.Equals(SortDirection?.ToLowerInvariant(), "desc", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the response should be compressed.
    /// </summary>
    public bool ShouldCompress => BatchSize > 100 || (MaxItems ?? 0) > 1000;
}