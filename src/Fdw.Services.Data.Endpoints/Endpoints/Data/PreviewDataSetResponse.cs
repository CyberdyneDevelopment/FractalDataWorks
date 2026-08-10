using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Response for a DataSet preview request.
/// </summary>
public sealed class PreviewDataSetResponse
{
    /// <summary>Gets or sets the column definitions.</summary>
    public IReadOnlyList<PreviewColumnDto> Columns { get; set; } = Array.Empty<PreviewColumnDto>();

    /// <summary>Gets or sets the preview data rows (field name to value).</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();

    /// <summary>Gets or sets whether more rows exist beyond MaxRows.</summary>
    public bool HasMoreRows { get; set; }

    /// <summary>Gets or sets the DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;
}
