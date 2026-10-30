using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One page of a data set read through <see cref="DataSetQuery"/>: the data set's declared columns and the rows that carry them.</summary>
public sealed class DataQueryPage
{
    /// <summary>Gets or sets the name of the data set read.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the columns every row carries: the data set's declared fields, or the group and measure columns of a grouped read.</summary>
    public IReadOnlyList<DataQueryColumnDto> Columns { get; set; } = [];

    /// <summary>Gets or sets the rows, each holding only the columns in <see cref="Columns"/>.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Gets or sets the page size the rows were cut to.</summary>
    public int Take { get; set; }

    /// <summary>Gets or sets a value indicating whether the store held more rows past this page.</summary>
    public bool HasMoreRows { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the page was cut in memory from every row the store's part of the query
    /// kept, because the filter or sort names a calculated field (FDW-1045). Such a read holds at most the row cap, so a
    /// caller wanting every row asks for the cap and has them in one read.
    /// </summary>
    public bool InMemory { get; set; }

    /// <summary>Gets or sets the number of groups in all, for a grouped read; null otherwise or when it cannot be known.</summary>
    public int? GroupCount { get; set; }
}
