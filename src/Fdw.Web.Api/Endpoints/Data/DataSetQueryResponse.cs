using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Response DTO for DataSet query endpoints (GET and POST variants).
/// </summary>
public sealed class DataSetQueryResponse
{
    /// <summary>Gets or sets the queried DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column metadata.</summary>
    public IReadOnlyList<DataSetQueryColumnDto> Columns { get; set; } = [];

    /// <summary>Gets or sets the result rows as field-name → value dictionaries.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Gets or sets the number of rows skipped.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the requested page size.</summary>
    public int Take { get; set; }

    /// <summary>Gets or sets whether more rows exist beyond this page.</summary>
    public bool HasMoreRows { get; set; }

    /// <summary>Gets or sets the filters that were applied.</summary>
    public IReadOnlyDictionary<string, string> AppliedFilters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
