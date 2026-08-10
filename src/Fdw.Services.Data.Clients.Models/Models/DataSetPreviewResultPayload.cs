using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Result payload from an incremental preview operation executed by the DataSet workbench.
/// </summary>
public sealed class DataSetPreviewResultPayload
{
    /// <summary>Gets or sets the preview rows, each expressed as a column-name-to-value map.</summary>
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Gets or sets the number of fields (columns) in the preview.</summary>
    public int FieldCount { get; set; }

    /// <summary>Gets or sets the number of rows returned in this preview batch.</summary>
    public int RowCount { get; set; }

    /// <summary>Gets or sets the estimated byte size of the result set.</summary>
    public long EstimatedSize { get; set; }

    /// <summary>Gets or sets the server-side execution time in milliseconds.</summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets whether the preview was truncated because it hit the row limit.
    /// When <c>true</c> the full result set contains more rows than are shown.
    /// </summary>
    public bool IsIncomplete { get; set; }
}
