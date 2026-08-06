namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The specialization of <see cref="IRecordSource{T}"/> that additionally exposes the positional cursor
/// (<see cref="Cursor"/>) driving its record enumeration — used both by genuinely tabular sources
/// (Delimited, FixedWidth, Tabular result sets, ADO.NET DataReader), where every record has the same
/// positional field set, and by Json, whose cursor tracks whatever properties the source object actually
/// carries (a possible superset of the declared schema).
/// </summary>
/// <remarks>
/// Why a distinct child interface: Xml (an item source with a genuinely non-tabular document shape) yields
/// records that are NOT rows and does NOT expose a cursor, so it implements <see cref="IRecordSource{T}"/>
/// only. Row sources (and Json) additionally expose the positional cursor (<see cref="Cursor"/>) so
/// consumers that want ordinal-level access (bulk copy, column projection, or — for Json specifically —
/// full-row extraction beyond the declared schema) can read cells without materializing a
/// <see cref="DataRecord"/> per row. Json's own <c>Read()</c> enumeration still yields schema-projected
/// <see cref="DataRecord"/>s (see <see cref="CursorRecordSource"/>) — exposing <see cref="Cursor"/> does
/// not change that; it adds an ADDITIONAL full-row access path a caller can opt into.
/// <para>
/// The write-side mirror is <see cref="IRowWriter"/> (a row writer), parallel to the item-writer parent
/// <see cref="IRecordWriter{T}"/>.
/// </para>
/// </remarks>
public interface IRowSource : IRecordSource<DataRecord>
{
    /// <summary>
    /// Gets the underlying positional cursor over the current row, for ordinal-level access without
    /// materializing a <see cref="DataRecord"/>. The cursor is advanced by the enumerators returned from
    /// the source's <c>Read</c> overloads; reading the cursor outside an active enumeration is undefined.
    /// </summary>
    IRecordCursor Cursor { get; }
}
