using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A script that is a query, read as a stream (FDW-4903): it answers a record source that reads the rows the text selects as
/// they are asked for, instead of the whole result at once as <see cref="IScriptReadCommand"/> does.
/// </summary>
/// <remarks>
/// <para>
/// The answer is the same streaming contract the data gateway's cursor has: an <see cref="IRecordSource{T}"/> of
/// <see cref="DataRecord"/> that owns the connection and the reader while it is read. The caller must dispose it; disposing it
/// releases them, whether the read ended, failed or was stopped early.
/// </para>
/// <para>
/// A failure during the read arrives as a failed record, and the read ends after it. One example is a row past
/// <see cref="Take"/>. Another is the store failing part way through. Rows read before it have already been handed on, so a
/// caller that has passed them on must treat the failed record as the failure of the whole read.
/// </para>
/// </remarks>
public interface IScriptStreamCommand : IScriptedDataCommand<IRecordSource<DataRecord>>
{
    /// <summary>
    /// Gets the most rows to read. The row after the last one allowed is a failed record that ends the read, so a script meant to
    /// select a few rows cannot load a table. Null states no limit.
    /// </summary>
    int? Take { get; }
}
