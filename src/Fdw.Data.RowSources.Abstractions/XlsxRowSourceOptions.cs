using System.Collections.Generic;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Abstractions;

/// <summary>Options for the Excel workbook (.xlsx) reader.</summary>
public sealed class XlsxRowSourceOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the names of the columns, left to right from column A. A row is read as far as these go: a cell
    /// further right is not read. At least one is required, as for the delimited reader.
    /// </summary>
    public IList<string> Columns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the first non-empty row is a header and is skipped. The names come from
    /// <see cref="Columns"/> either way.
    /// </summary>
    public bool HasHeader { get; set; }

    /// <summary>
    /// Gets or sets the name of the worksheet to read, compared ignoring case. Null is allowed only for a workbook with
    /// exactly one worksheet: with several, the reader refuses and names them rather than picking one.
    /// </summary>
    public string? SheetName { get; set; }
}
