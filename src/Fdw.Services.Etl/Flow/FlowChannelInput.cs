using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The input node a component reads: batches from the channel every incoming link writes to (FDW-1386).
/// </summary>
internal sealed class FlowChannelInput : IFlowInput
{
    private readonly FlowInputState _state;

    /// <summary>Initializes a new instance of the <see cref="FlowChannelInput"/> class.</summary>
    /// <param name="state">The input's state.</param>
    public FlowChannelInput(FlowInputState state)
    {
        _state = state;
    }

    /// <inheritdoc />
    public string Name => _state.Name;

    /// <inheritdoc />
    public async Task<FlowBatch?> Read(CancellationToken cancellationToken = default)
    {
        while (await _state.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_state.Reader.TryRead(out var batch))
            {
                return batch;
            }
        }

        return null;
    }

    /// <summary>Reads and discards what is left, so a writer waiting on a full channel is released.</summary>
    /// <param name="cancellationToken">Stops the drain when the run is cancelled.</param>
    /// <returns>The number of rows discarded.</returns>
    public async Task<long> Drain(CancellationToken cancellationToken)
    {
        long discarded = 0;
        while (await Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            discarded += batch.Count;
        }

        return discarded;
    }
}
