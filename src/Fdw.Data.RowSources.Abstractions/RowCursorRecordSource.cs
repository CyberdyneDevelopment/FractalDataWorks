using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The row-oriented record source: a <see cref="CursorRecordSource"/> that additionally implements
/// <see cref="IRowSource"/> by surfacing its underlying cursor. Used by the genuinely tabular formats
/// (Delimited, FixedWidth, Tabular, DataReader) so consumers can read cells by ordinal as well as
/// enumerate <see cref="DataRecord"/> rows.
/// </summary>
/// <remarks>
/// Item formats (Json/Xml) use the base <see cref="CursorRecordSource"/> directly and do NOT expose a
/// cursor, because their records are items, not positional rows.
/// </remarks>
public sealed class RowCursorRecordSource : CursorRecordSource, IRowSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowCursorRecordSource"/> class.
    /// </summary>
    /// <param name="reader">The low-level row cursor/reader to project rows from.</param>
    /// <param name="fields">The container's field children — the flyweight schema for produced rows.</param>
    /// <param name="logger">Logger for record-source diagnostics; falls back to a null logger when null.</param>
    public RowCursorRecordSource(IRowSourceReader reader, IReadOnlyList<IDataField> fields, ILogger? logger = null)
        : base(reader, fields, logger)
    {
    }

    /// <inheritdoc />
    public IRecordCursor Cursor => Reader;
}
