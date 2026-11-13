using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>A stable read that never hands out a row and ends only when it is cancelled: a source that is slower than any limit.</summary>
internal sealed class BlockingRecordSource : IRecordSource<DataRecord>
{
    /// <inheritdoc />
    public RecordSchema Schema { get; } = new([]);

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read() => [];

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        yield break;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
