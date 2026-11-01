using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Response DTO for a DataSet data preview, returned by <c>POST /datasets/{name}/preview</c>.
/// The JSON shape matches <c>DataPreviewResponse</c> in the client package for transparent deserialization.
/// </summary>
public class DataSetPreviewResponseDto
{
    /// <summary>Gets or sets the column definitions inferred from the result set.</summary>
    public IList<DataSetPreviewColumnDto> Columns { get; set; } = [];

    /// <summary>Gets or sets the data rows as key-value dictionaries.</summary>
    public IList<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Gets or sets the estimated total row count (for pagination display).</summary>
    public long? TotalRowCount { get; set; }

    /// <summary>Gets or sets a value indicating whether additional rows exist beyond the current page.</summary>
    public bool HasMoreRows { get; set; }
}

