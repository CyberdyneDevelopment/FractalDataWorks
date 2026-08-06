using System.Collections.Generic;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Response for data preview.
/// </summary>
public class DataPreviewResponse
{
    /// <summary>
    /// Gets or sets the source description.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column definitions.
    /// </summary>
    public IList<PreviewColumnDto> Columns { get; set; } = [];

    /// <summary>
    /// Gets or sets the data rows.
    /// </summary>
    public IList<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Gets or sets the total row count returned.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Gets or sets whether there are more rows available.
    /// </summary>
    public bool HasMoreRows { get; set; }

    /// <summary>
    /// Gets or sets any warnings or info messages.
    /// </summary>
    public IList<string> Messages { get; set; } = [];
}
