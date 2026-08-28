using System.Collections.Generic;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base options for row writer configuration. The write-side mirror of <see cref="RowSourceOptions"/>.
/// </summary>
/// <remarks>
/// Format-specific writers (delimited, fixed-width, JSON, XML) derive from this base to carry
/// the knobs of their underlying serializer library. The base only holds the column ordering that
/// every text writer needs; format-specific knobs (separator, padding, element names) live on the
/// derived option classes co-located with each writer.
/// </remarks>
public class RowWriterOptions
{
    /// <summary>
    /// Gets or sets the explicit ordered list of column/field names to emit.
    /// When empty, text writers that require a fixed column order (delimited, fixed-width) must
    /// fail loud rather than guess an order from the first row.
    /// </summary>
    public IList<string> Columns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the buffer size for streaming write operations. Default is 16KB.
    /// </summary>
    public int BufferSize { get; set; } = 16 * 1024;
}
