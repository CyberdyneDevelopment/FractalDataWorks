using System.Collections.Generic;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.FixedWidth.Abstractions;

/// <summary>
/// Options for fixed-width (fixed-length) row reading. The per-field offsets/widths drive
/// RecordParser's fixed-length slicing; reading uses RecordParser's raw line reader and slices each
/// line per <see cref="Fields"/>.
/// </summary>
public sealed class FixedWidthRowSourceOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the ordered fixed-width field definitions. Required — a fixed-width reader cannot
    /// slice columns without knowing their offsets and widths.
    /// </summary>
    public IList<FixedWidthField> Fields { get; set; } = new List<FixedWidthField>();

    /// <summary>
    /// Gets or sets whether the first line is a header row to skip. Default is false.
    /// </summary>
    public bool HasHeader { get; set; }

    /// <summary>
    /// Gets or sets whether to trim padding characters from each sliced field value. Default is true.
    /// </summary>
    public bool Trim { get; set; } = true;
}
