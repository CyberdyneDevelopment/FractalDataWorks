using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Base class for HTTP-based row enumerators that stream paginated responses.
/// </summary>
public abstract class HttpRowEnumeratorBase : IRowEnumerator
{
    private long _rowsRead;
    private long _rowErrors;
    private bool _disposed;

    /// <inheritdoc />
    public long RowsRead => _rowsRead;

    /// <inheritdoc />
    public long RowErrors => _rowErrors;

    /// <inheritdoc />
    public abstract IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumerateRows(
        IRowMapper mapper,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the rows read counter.
    /// </summary>
    protected void IncrementRowsRead()
    {
        Interlocked.Increment(ref _rowsRead);
    }

    /// <summary>
    /// Increments the row errors counter.
    /// </summary>
    protected void IncrementRowErrors()
    {
        Interlocked.Increment(ref _rowErrors);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
        return DisposeAsyncCore();
    }

    /// <summary>
    /// Override to add custom disposal logic.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return default;
    }
}
