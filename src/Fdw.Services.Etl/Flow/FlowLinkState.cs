using System.Threading;
using System.Threading.Channels;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One link in a run: the channel its rows travel on and what has moved over it (FDW-1386).
/// </summary>
internal sealed class FlowLinkState
{
    private long _rows;
    private long _batches;

    /// <summary>Initializes a new instance of the <see cref="FlowLinkState"/> class.</summary>
    /// <param name="link">The link; for a dropped output its target names are empty.</param>
    /// <param name="input">The state of the input the link feeds; null for a dropped output.</param>
    public FlowLinkState(FlowLink link, FlowInputState? input)
    {
        Link = link;
        Input = input;
    }

    /// <summary>Gets the link.</summary>
    public FlowLink Link { get; }

    /// <summary>Gets the input this link feeds; null when the output is dropped.</summary>
    public FlowInputState? Input { get; }

    /// <summary>Gets the writer of the channel the link feeds; null when the output is dropped.</summary>
    public ChannelWriter<FlowBatch>? Writer => Input?.Writer;

    /// <summary>Gets what moved over the link.</summary>
    public FlowLinkCount Count => new(Link, Interlocked.Read(ref _rows), Interlocked.Read(ref _batches));

    /// <summary>Counts a batch written to the link.</summary>
    /// <param name="rows">The number of rows in it.</param>
    public void Add(int rows)
    {
        Interlocked.Add(ref _rows, rows);
        Interlocked.Increment(ref _batches);
    }
}
