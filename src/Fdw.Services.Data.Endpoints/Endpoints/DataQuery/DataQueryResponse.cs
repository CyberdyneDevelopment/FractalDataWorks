using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Response for <c>POST /data/{DataSetName}</c>: one page of a data set's rows.</summary>
public class DataQueryResponse
{
    /// <summary>Gets or sets the data set queried.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set's fields, in ordinal order.</summary>
    public IReadOnlyList<DataQueryColumnDto> Columns { get; set; } = [];

    /// <summary>Gets or sets the rows, each keyed by field name.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Gets or sets the rows skipped.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the page size asked for.</summary>
    public int Take { get; set; }

    /// <summary>Gets or sets whether at least one more row follows this page.</summary>
    public bool HasMoreRows { get; set; }

    /// <summary>
    /// Gets or sets the number of groups in all, before the page, on a grouped query; null on any other, and
    /// null on a grouped page past the last group, which carries no row to read it from.
    /// </summary>
    public int? GroupCount { get; set; }
}
