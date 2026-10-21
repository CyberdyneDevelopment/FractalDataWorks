using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Collects the rows a component builds and writes them to an output node in batches of a stated size (FDW-1386).
/// </summary>
internal sealed class FlowBatchWriter
{
    private readonly IFlowOutput _output;
    private readonly int _batchSize;
    private readonly CancellationToken _cancellationToken;
    private List<IDictionary<string, object?>> _rows;

    /// <summary>Initializes a new instance of the <see cref="FlowBatchWriter"/> class.</summary>
    /// <param name="output">The output node.</param>
    /// <param name="batchSize">How many rows go in a batch; at least one.</param>
    /// <param name="cancellationToken">Cancels a write that is waiting on a full link.</param>
    public FlowBatchWriter(IFlowOutput output, int batchSize, CancellationToken cancellationToken)
    {
        _output = output;
        _batchSize = batchSize;
        _cancellationToken = cancellationToken;
        _rows = new List<IDictionary<string, object?>>(batchSize);
    }

    /// <summary>Adds a row; the batch is written when it is full.</summary>
    /// <param name="row">The row.</param>
    /// <returns>A task that completes when the row is held or its batch is written.</returns>
    public async Task Add(IDictionary<string, object?> row)
    {
        _rows.Add(row);
        if (_rows.Count >= _batchSize)
        {
            await Send().ConfigureAwait(false);
        }
    }

    /// <summary>Writes the rows still held.</summary>
    /// <returns>Success.</returns>
    public async Task<IGenericResult> Flush()
    {
        if (_rows.Count > 0)
        {
            await Send().ConfigureAwait(false);
        }

        return GenericResult.Success();
    }

    private async Task Send()
    {
        var batch = new FlowBatch(_rows);
        _rows = new List<IDictionary<string, object?>>(_batchSize);
        await _output.Write(batch, _cancellationToken).ConfigureAwait(false);
    }
}
