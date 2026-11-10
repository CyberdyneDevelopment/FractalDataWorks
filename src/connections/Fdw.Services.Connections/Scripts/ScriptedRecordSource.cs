using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Data.DataNodes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Scripts;

/// <summary>
/// The rows a script selects, streamed from the open reader (FDW-4903): each row is read when it is asked for, and the reader,
/// its command and its connection stay open until the source is disposed.
/// </summary>
/// <remarks>
/// <para>
/// Built by <see cref="ScriptedRows.Open"/> once the connection has run the script, so every SQL connection streams the same way
/// and adds only its driver. The schema is the result's own columns: a script declares no container, so each column is a field
/// that nobody has typed yet (<c>NotYetTyped</c>), named as the result names it.
/// </para>
/// <para>
/// A failure ends the read as one failed record. That is a row past the most the script may read, or the store failing part way
/// through, translated by the connection that ran it. Cancellation stays an <see cref="OperationCanceledException"/>. Whatever
/// ends the read (its end, a failure, a cancellation or the caller stopping early), the reader, command and connection are
/// released, and disposing the source releases them too.
/// </para>
/// </remarks>
public sealed class ScriptedRecordSource : IRecordSource<DataRecord>
{
    private readonly DbDataReader _reader;
    private readonly IReadOnlyList<IDisposable> _owned;
    private readonly int? _take;
    private readonly Func<Exception, IEnumerable<IGenericMessage>> _failed;
    private readonly ILogger _logger;
    private int _read;
    private int _released;

    /// <summary>Initializes a new instance of the <see cref="ScriptedRecordSource"/> class.</summary>
    /// <param name="reader">The open reader.</param>
    /// <param name="owned">What the source releases when it ends, in order: the reader, then its command, then its connection.</param>
    /// <param name="take">The most rows to read; null states no limit.</param>
    /// <param name="failed">Says what a failure of the store while reading is, as the connection that ran the script says it.</param>
    /// <param name="logger">The logger.</param>
    internal ScriptedRecordSource(
        DbDataReader reader,
        IReadOnlyList<IDisposable> owned,
        int? take,
        Func<Exception, IEnumerable<IGenericMessage>> failed,
        ILogger logger)
    {
        _reader = reader;
        _owned = owned;
        _take = take;
        _failed = failed;
        _logger = logger;
        Schema = new RecordSchema([.. Enumerable.Range(0, reader.FieldCount).Select(Column)]);
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <inheritdoc />
    /// <remarks>A streamed script is read from a network reader, so it is read asynchronously only; this answers one failed record.</remarks>
    public IEnumerable<IGenericResult<DataRecord>> Read()
    {
        yield return GenericResult<DataRecord>.Failure(ScriptedRowsLog.ReadOnlyAsynchronously(_logger));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _read, 1) == 1)
        {
            yield return GenericResult<DataRecord>.Failure(ScriptedRowsLog.ReadAgain(_logger));
            yield break;
        }

        try
        {
            var rows = 0;
            while (await Next(rows, cancellationToken).ConfigureAwait(false) is { } record)
            {
                yield return record;
                if (record.IsFailure)
                {
                    yield break;
                }

                rows++;
            }

            ScriptedRowsLog.Read(_logger, rows);
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _released, 1) == 1)
        {
            return;
        }

        foreach (var owned in _owned)
        {
            owned.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) == 1)
        {
            return;
        }

        foreach (var owned in _owned)
        {
            if (owned is IAsyncDisposable releasing)
            {
                await releasing.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                owned.Dispose();
            }
        }
    }

    // A column of the result as a field: its name and the store's name for its type, and no datum type, because nobody has said
    // what a script's column is. Why nullable: nothing declares a script's column not null, and a database null is read as null.
    private IDataField Column(int ordinal) =>
        new DataField(_reader.GetName(ordinal), null, null, ordinal, true, DatumTypes.NotYetTyped, _reader.GetDataTypeName(ordinal));

    // The next row, a failed record that ends the read, or null at the end. Why apart from Read: a yield cannot sit inside a
    // try that catches, and a failure of the store must become a failed record rather than leave the read as an exception.
    private async Task<IGenericResult<DataRecord>?> Next(int rows, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            if (_take is { } most && rows >= most)
            {
                return GenericResult<DataRecord>.Failure(ScriptedRowsLog.TooManyRows(_logger, most));
            }

            var values = new object?[_reader.FieldCount];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = await _reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : _reader.GetValue(i);
            }

            return GenericResult<DataRecord>.Success(new DataRecord(Schema, values));
        }
        catch (Exception ex) when (ex is not OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            // Why: a driver can answer a cancelled read with its own exception; the read was cancelled, so it ends as one.
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<DataRecord>.Failure(_failed(ex).Prepend(ScriptedRowsLog.ReadFailedPartWay(_logger, rows)));
        }
    }
}
