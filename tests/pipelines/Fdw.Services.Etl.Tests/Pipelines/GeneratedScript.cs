using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Data.DataNodes;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A script that selects a large number of rows, made one at a time as they are asked for and never held (FDW-4903), so a test can
/// stream more rows than it would want in memory. Each row is its <c>Id</c>, from zero up. Before each row goes out it tells the
/// test how many rows it has handed out, so the test can see how many are in flight.
/// </summary>
internal sealed class GeneratedScript : IRecordSource<DataRecord>
{
    private readonly int _rows;
    private readonly Action<int> _handingOut;

    /// <summary>Initializes a new instance of the <see cref="GeneratedScript"/> class.</summary>
    /// <param name="rows">How many rows the script selects.</param>
    /// <param name="handingOut">Told the number of rows handed out so far, including the one about to go.</param>
    public GeneratedScript(int rows, Action<int> handingOut)
    {
        _rows = rows;
        _handingOut = handingOut;
        Schema = new RecordSchema([new DataField("Id", null, null, 0, false, DatumTypes.NotYetTyped)]);
    }

    /// <summary>Gets a value indicating whether the source was released.</summary>
    public bool Released { get; private set; }

    public RecordSchema Schema { get; }

    public IEnumerable<IGenericResult<DataRecord>> Read() => throw new NotSupportedException("read the script asynchronously");

    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var id = 0; id < _rows; id++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _handingOut(id + 1);
            yield return GenericResult<DataRecord>.Success(new DataRecord(Schema, [id]));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public void Dispose() => Released = true;

    public ValueTask DisposeAsync()
    {
        Released = true;
        return ValueTask.CompletedTask;
    }
}
