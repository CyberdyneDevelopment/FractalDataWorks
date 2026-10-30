using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The output node a component writes: every batch goes to every link of the node, each after the first as a copy (FDW-1386).
/// </summary>
/// <remarks>
/// Writing waits while a receiving link is full. That wait is the back-pressure of the run. A node with no links is a dropped
/// output: its rows are counted and discarded.
/// </remarks>
internal sealed class FlowChannelOutput : IFlowOutput
{
    private readonly IReadOnlyList<FlowLinkState> _links;
    private readonly string _component;
    private readonly FlowRejectedRows? _rejected;
    private readonly IFlowPauseGate? _pauseGate;

    /// <summary>Initializes a new instance of the <see cref="FlowChannelOutput"/> class.</summary>
    /// <param name="name">The output node's name.</param>
    /// <param name="links">The links that start at it.</param>
    /// <param name="component">The component the node belongs to.</param>
    /// <param name="rejected">Where the rows written to this node are kept, when it is an error output of a run that keeps them (FDW-1720); none otherwise.</param>
    /// <param name="pauseGate">The gate a batch passes before it leaves the component; none for a run that cannot be paused (FDW-2950).</param>
    public FlowChannelOutput(string name, IReadOnlyList<FlowLinkState> links, string component, FlowRejectedRows? rejected, IFlowPauseGate? pauseGate)
    {
        _pauseGate = pauseGate;
        Name = name;
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _rejected = rejected;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task Write(FlowBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        // Why before the batch leaves: a paused run holds every component at the point it would pass rows on, so nothing new enters the
        // run while it is paused and every batch already passed on is finished (FDW-2950).
        if (_pauseGate is not null)
        {
            await _pauseGate.WaitWhilePaused(cancellationToken).ConfigureAwait(false);
        }

        // Why before the links: a node with no links drops its rows, and a rejected row that is dropped is the one the record must keep.
        _rejected?.Keep(_component, Name, batch.Rows);
        for (var i = 0; i < _links.Count; i++)
        {
            var link = _links[i];
            if (link.Writer is not null)
            {
                await link.Writer.WriteAsync(i == 0 ? batch : batch.Copy(), cancellationToken).ConfigureAwait(false);
            }

            link.Add(batch.Count);
        }
    }

    /// <summary>Tells every receiving input that this output will send nothing more.</summary>
    public void Finish()
    {
        foreach (var link in _links)
        {
            link.Input?.LinkFinished();
        }
    }
}
