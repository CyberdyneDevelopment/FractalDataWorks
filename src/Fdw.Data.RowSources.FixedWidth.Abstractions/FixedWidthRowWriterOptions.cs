using System.Collections.Generic;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.FixedWidth.Abstractions;

/// <summary>
/// Options for fixed-width (fixed-length) row writing. The write-side mirror of
/// <see cref="FixedWidthRowSourceOptions"/>. The per-field offsets/widths/padding map directly to
/// RecordParser's fixed-length writer <c>Map(expr, startIndex, length, padding, paddingChar)</c>.
/// </summary>
public sealed class FixedWidthRowWriterOptions : RowWriterOptions
{
    /// <summary>
    /// Gets or sets the ordered fixed-width field definitions. Required.
    /// </summary>
    // Why: NO FALLBACKS — field layout comes from the container schema, never guessed.
    // IList (mutable) for option binding/assignment; backed by List.
    public IList<FixedWidthField> Fields { get; set; } = new List<FixedWidthField>();

    /// <summary>
    /// Gets or sets whether to emit a header row of field names before the data rows. Default false.
    /// </summary>
    public bool WriteHeader { get; set; }
}
