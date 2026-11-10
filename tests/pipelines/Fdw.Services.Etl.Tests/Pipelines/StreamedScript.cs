using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Scripts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A script's streamed rows for source tests (FDW-4903). It reads the rows it was given through the shared script reader, and it
/// can hold back the rows after a stated count until a gate opens, or end with a failed record after them. It counts the rows it
/// handed out and says whether it was released.
/// </summary>
internal sealed class StreamedScript : IRecordSource<DataRecord>
{
    private readonly IRecordSource<DataRecord> _inner;
    private readonly int? _holdAfter;
    private readonly Task? _gate;
    private readonly int? _failAfter;
    private readonly IGenericMessage? _failure;

    /// <summary>Initializes a new instance of the <see cref="StreamedScript"/> class.</summary>
    /// <param name="rows">The rows the script selects.</param>
    /// <param name="take">The most rows the read allows; none states no limit.</param>
    /// <param name="holdAfter">How many rows go out before the rest wait for <paramref name="gate"/>; none holds nothing back.</param>
    /// <param name="gate">What the held rows wait for.</param>
    /// <param name="failAfter">How many rows go out before the read ends with <paramref name="failure"/>; none never fails.</param>
    /// <param name="failure">The message of the failed record.</param>
    public StreamedScript(
        IReadOnlyList<IDictionary<string, object?>> rows,
        int? take = null,
        int? holdAfter = null,
        Task? gate = null,
        int? failAfter = null,
        IGenericMessage? failure = null)
    {
        var table = new DataTable();
        foreach (var column in rows.SelectMany(row => row.Keys).Distinct(System.StringComparer.OrdinalIgnoreCase))
            table.Columns.Add(column, typeof(object));
        if (table.Columns.Count == 0)
            table.Columns.Add("Id", typeof(object));
        foreach (var row in rows)
            table.Rows.Add([.. table.Columns.Cast<DataColumn>().Select(column => row.TryGetValue(column.ColumnName, out var value) ? value : null)]);

        var reader = table.CreateDataReader();
        var opened = ScriptedRows.Open(reader, [reader], take, exception => [new GenericMessage(exception.Message)], NullLogger.Instance);
        _inner = opened.Value!;
        _holdAfter = holdAfter;
        _gate = gate;
        _failAfter = failAfter;
        _failure = failure;
    }

    /// <summary>Gets how many rows were handed out.</summary>
    public int HandedOut { get; private set; }

    /// <summary>Gets a value indicating whether the source was released.</summary>
    public bool Released { get; private set; }

    public RecordSchema Schema => _inner.Schema;

    public IEnumerable<IGenericResult<DataRecord>> Read() => _inner.Read();

    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var record in _inner.Read(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (HandedOut == _failAfter && _failure is not null)
            {
                yield return GenericResult<DataRecord>.Failure(_failure);
                yield break;
            }

            if (HandedOut == _holdAfter && _gate is not null)
                await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

            HandedOut++;
            yield return record;
        }
    }

    public void Dispose()
    {
        Released = true;
        _inner.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Released = true;
        await _inner.DisposeAsync().ConfigureAwait(false);
    }
}
