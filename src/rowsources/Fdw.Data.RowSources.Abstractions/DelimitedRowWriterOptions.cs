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

    /// <summary>
    /// Gets or sets a value indicating whether the output starts with a UTF-8 byte order mark (U+FEFF).
    /// </summary>
    /// <remarks>
    /// For a file a person opens in Excel: without it Excel reads the file in the machine's ANSI code page and garbles every
    /// character outside ASCII. Off by default, because a machine consumer would read the mark as part of the first column name.
    /// </remarks>
    public bool WriteByteOrderMark { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a text value that starts like a formula is written with a leading apostrophe.
    /// </summary>
    /// <remarks>
    /// For a file a person opens in a spreadsheet, which reads a cell starting with <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, a tab or a
    /// carriage return as a formula and runs it (CSV injection). Only text is changed: a number or a date is never a formula. Off by
    /// default, because it alters the value, which a machine consumer must not see.
    /// </remarks>
    public bool NeutralizeFormulas { get; set; }
}
