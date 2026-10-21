using System.Threading;
using System.Threading.Channels;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One input node in a run: the channel every link into it writes to, and how many of those links have not finished (FDW-1386).
/// </summary>
internal sealed class FlowInputState
{
    private int _unfinished;

    /// <summary>Initializes a new instance of the <see cref="FlowInputState"/> class.</summary>
    /// <param name="name">The input node's name.</param>
    /// <param name="capacity">How many batches the channel buffers.</param>
    /// <param name="links">How many links end at this input.</param>
    /// <param name="unbounded">Whether the channel buffers without limit, for a line from a lower stage whose reader has not started (FDW-2040).</param>
    public FlowInputState(string name, int capacity, int links, bool unbounded = false)
    {
        Name = name;
        _unfinished = links;

        // Why unbounded for a line that crosses stages (FDW-2040): the writer is in an earlier stage than the reader, so the reader has not started and cannot
        // make room; a bounded buffer would stop the writer, the stage could never finish and the later stage would never start. The rows wait in memory.
        var channel = unbounded
            ? Channel.CreateUnbounded<FlowBatch>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false })
            : Channel.CreateBounded<FlowBatch>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        Reader = channel.Reader;
        Writer = channel.Writer;
    }

    /// <summary>Gets the node's name.</summary>
    public string Name { get; }

    /// <summary>Gets the end the component reads.</summary>
    public ChannelReader<FlowBatch> Reader { get; }

    /// <summary>Gets the end the links write.</summary>
    public ChannelWriter<FlowBatch> Writer { get; }

    /// <summary>Records that one link into this input will send nothing more; the channel completes with the last.</summary>
    public void LinkFinished()
    {
        if (Interlocked.Decrement(ref _unfinished) == 0)
        {
            Writer.TryComplete();
        }
    }
}
