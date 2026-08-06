namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Synchronous reader that extends the <see cref="IRecordCursor"/> primitive with forward navigation.
/// Use for data sources that support synchronous reading (IDataReader, in-memory collections).
/// </summary>
public interface IRowSourceReader : IRecordCursor
{
    /// <summary>
    /// Advances to the next row.
    /// </summary>
    /// <returns>True if there is another row; false if at end of data.</returns>
    bool Read();

    /// <summary>
    /// Resets the reader to before the first row, if supported.
    /// </summary>
    /// <remarks>
    /// Not all sources support reset (e.g., forward-only network streams).
    /// Check <see cref="CanReset"/> before calling.
    /// </remarks>
    void Reset();

    /// <summary>
    /// Gets whether this source supports resetting to the beginning.
    /// </summary>
    bool CanReset { get; }
}
