using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Abstractions;

/// <summary>
/// Options for delimited (variable-length) row writing. The write-side mirror of
/// <see cref="DelimitedRowSourceOptions"/>. The column ordering comes from the inherited
/// <see cref="RowWriterOptions.Columns"/>; the remaining knobs control RecordParser-compatible
/// line formatting (separator, quoting, header emission).
/// </summary>
public sealed class DelimitedRowWriterOptions : RowWriterOptions
{
    /// <summary>
    /// Gets or sets the column separator string. Default is ",".
    /// Mirrors <see cref="DelimitedRowSourceOptions.Separator"/>.
    /// </summary>
    public string Separator { get; set; } = ",";

    /// <summary>
    /// Gets or sets the quote character used to enclose fields containing the separator, the quote,
    /// or a line break. Default is '"'. Embedded quotes are doubled, matching RecordParser's
    /// <c>ContainsQuotedFields</c> read convention.
    /// </summary>
    public char QuoteChar { get; set; } = '"';

    /// <summary>
    /// Gets or sets whether to emit a header row of column names before the data rows.
    /// Default is false. Mirrors <see cref="DelimitedRowSourceOptions.HasHeader"/>.
    /// </summary>
    public bool WriteHeader { get; set; }
}
