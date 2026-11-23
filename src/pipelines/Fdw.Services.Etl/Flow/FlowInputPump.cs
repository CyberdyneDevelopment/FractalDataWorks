using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Reads an input node into an unbounded buffer on its own task, so a component that must finish reading one input before it
/// uses the other cannot deadlock the run (FDW-1386).
/// </summary>
/// <remarks>
/// Why it exists: a join or a lookup builds from one input and streams the other. When both inputs come from one upstream
/// output (a multicast), that upstream writes to one link, then the other; if the component reads only the build input and the
/// upstream is blocked writing the streaming one, nothing moves. The pump keeps taking the streaming input while the build input
/// is read; what it holds is only the rows that arrive before the build finishes. Those rows are held by the component: each batch counts on its gauge from the
/// moment the pump takes it until the component reads it (FDW-5231).
/// </remarks>
internal sealed class FlowInputPump
{
    private readonly FlowHeldGauge? _held;

    private FlowInputPump(ChannelReader<FlowBatch> reader, Task completion, FlowHeldGauge? held)
    {
        Reader = reader;
        Completion = completion;
        _held = held;
    }

    /// <summary>Gets the buffer the pumped batches are read from.</summary>
    public ChannelReader<FlowBatch> Reader { get; }

    /// <summary>Gets the task that ends when the input has been read to its end.</summary>
    public Task Completion { get; }

    /// <summary>Starts reading an input.</summary>
    /// <param name="input">The input node.</param>
    /// <param name="held">The gauge of the component the pump reads for, where a buffered batch counts as held; none when the ports carry no gauge.</param>
    /// <param name="cancellationToken">Stops the pump when the run is cancelled.</param>
    /// <returns>The running pump.</returns>
    public static FlowInputPump Start(IFlowInput input, FlowHeldGauge? held, CancellationToken cancellationToken)
    {
        var buffer = Channel.CreateUnbounded<FlowBatch>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        var completion = Task.Run(async () =>
        {
            try
            {
                while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
                {
                    held?.Hold(batch);
                    await buffer.Writer.WriteAsync(batch, cancellationToken).ConfigureAwait(false);
                }

                buffer.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                buffer.Writer.TryComplete(ex);
                throw;
            }
        }, CancellationToken.None);
        return new FlowInputPump(buffer.Reader, completion, held);
    }

    /// <summary>Reads the pumped batches in order, each let go from the component's gauge as it is read (FDW-5231).</summary>
    /// <param name="cancellationToken">Stops the read when the run is cancelled.</param>
    /// <returns>The batches.</returns>
    public async IAsyncEnumerable<FlowBatch> ReadAll([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var batch in Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            _held?.Release(batch);
            yield return batch;
        }
    }
}
