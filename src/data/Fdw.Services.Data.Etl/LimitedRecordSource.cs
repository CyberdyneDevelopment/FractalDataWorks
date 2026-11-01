using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Etl;

/// <summary>
/// Keeps a connection's limits in force for as long as a record-source cursor is open (FDW-947, FDW-949).
/// </summary>
/// <remarks>
/// <para>
/// <c>Execute</c> is finished by the time it returns, so a limit that applies while it runs can be
/// released in a <c>finally</c>. A cursor is not: the connection returns as soon as it is open and the
/// rows are read afterwards. So this decorator takes over what the gateway would have released -- the
/// lease holding the connection's concurrency slots, and the query timeout, whose timer started when the
/// cursor was opened -- and releases them when the caller disposes the cursor.
/// </para>
/// <para>
/// While a timeout is present, every enumeration is linked to it and ends with a timeout failure element
/// when it fires; a synchronous enumeration checks it between records.
/// </para>
/// </remarks>
internal sealed class LimitedRecordSource : IRecordSource<DataRecord>
{
    private readonly IRecordSource<DataRecord> _inner;
    private readonly ConnectionLimitLease _lease;
    private readonly CancellationTokenSource? _timeout;
    private readonly int _timeoutSeconds;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="LimitedRecordSource"/> class.</summary>
    /// <param name="inner">The connection's cursor.</param>
    /// <param name="lease">The lease holding the connection's concurrency slots; this instance takes ownership.</param>
    /// <param name="timeout">The timeout started when the cursor opened, or null when none applies; this instance takes ownership.</param>
    /// <param name="timeoutSeconds">The configured timeout, for the failure message.</param>
    /// <param name="logger">The logger.</param>
    public LimitedRecordSource(
        IRecordSource<DataRecord> inner,
        ConnectionLimitLease lease,
        CancellationTokenSource? timeout,
        int timeoutSeconds,
        ILogger logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _lease = lease ?? throw new ArgumentNullException(nameof(lease));
        _timeout = timeout;
        _timeoutSeconds = timeoutSeconds;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public RecordSchema Schema => _inner.Schema;

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read()
    {
        foreach (var record in _inner.Read())
        {
            if (_timeout is { IsCancellationRequested: true })
            {
                yield return TimedOut();
                yield break;
            }

            yield return record;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var linked = _timeout is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timeout.Token);
        await using var enumerator = _inner.Read(linked.Token).GetAsyncEnumerator(linked.Token);

        while (true)
        {
            var moved = false;
            var timedOut = false;

            try
            {
                moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex is not null && _timeout is { IsCancellationRequested: true } && !cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
            }

            if (timedOut)
            {
                yield return TimedOut();
                yield break;
            }

            if (!moved)
            {
                yield break;
            }

            yield return enumerator.Current;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _inner.Dispose();
        _timeout?.Dispose();
        _lease.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync().ConfigureAwait(false);
        _timeout?.Dispose();
        _lease.Dispose();
    }

    private IGenericResult<DataRecord> TimedOut()
        => GenericResult<DataRecord>.Failure(
            ConnectionLimitLog.QueryTimeoutExceeded(_logger, Guid.Empty, _timeoutSeconds));
}
