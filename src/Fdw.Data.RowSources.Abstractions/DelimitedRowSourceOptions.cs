using System.Collections.Generic;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Abstractions;

/// <summary>
/// Options for delimited (variable-length) row reading. Every knob maps 1:1 to a
/// <c>VariableLengthReaderRawOptions</c> setting on the underlying
/// RecordParser library, plus the column names used to project each line into a named row.
/// </summary>
public sealed class DelimitedRowSourceOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the ordered list of column names. The reader maps each parsed column position to
    /// the name at the same index. Required — a delimited reader cannot name columns it cannot see.
    /// </summary>
    // Why: RecordParser's raw reader produces positional columns (Func&lt;int,string&gt;); the column
    // names come from the container field schema, never inferred. IList&lt;string&gt; (mutable) for option binding; backed by List&lt;string&gt;.
    public IList<string> Columns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the column separator string (RecordParser <c>Separator</c>). Default is ",".
    /// </summary>
    public string Separator { get; set; } = ",";

    /// <summary>
    /// Gets or sets whether the first line is a header row to skip (RecordParser <c>HasHeader</c>).
    /// Default is false.
    /// </summary>
    public bool HasHeader { get; set; }

    /// <summary>
    /// Gets or sets whether fields may be quoted (RecordParser <c>ContainsQuotedFields</c>).
    /// Default is true.
    /// </summary>
    public bool ContainsQuotedFields { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to trim whitespace from each field (RecordParser <c>Trim</c>).
    /// Default is false.
    /// </summary>
    public bool Trim { get; set; }
}
