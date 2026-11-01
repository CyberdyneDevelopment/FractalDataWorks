using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Counts the rows of every flow graph a run carries out (FDW-1403, FDW-2041): it runs each graph through the engine's executor and adds up the
/// rows that left a source and the rows that reached a destination.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline runs one flow graph, and a loop runs its body's graph many times, so the run's counts are the sum across all of them. The rows of a
/// graph are counted as its components finish, so an action in a later stage of the same graph (a Notify) reports the rows of the stages before it.
/// A graph that fails adds nothing: the run is reported failed, not partly counted.
/// </para>
/// </remarks>
public sealed class FlowRunTally : IFlowExecutor, IFlowRunCounts
{
    private readonly IFlowExecutor _inner;
    private readonly List<Pending> _running = [];
    private long _extracted;
    private long _transformed;
    private long _rejected;

    /// <summary>Initializes a new instance of the <see cref="FlowRunTally"/> class.</summary>
    /// <param name="inner">The executor that runs each graph.</param>
    public FlowRunTally(IFlowExecutor inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>Gets the rows that left a component with no inputs, across the run, including those of the graphs running now.</summary>
    public long Extracted => Interlocked.Read(ref _extracted) + Running(pending => pending.Extracted);

    /// <summary>Gets the rows that reached a component with no outputs, across the run, including those of the graphs running now.</summary>
    public long Transformed => Interlocked.Read(ref _transformed) + Running(pending => pending.Transformed);

    /// <inheritdoc />
    public long Rejected => Interlocked.Read(ref _rejected) + Running(pending => pending.Rejected);

    /// <inheritdoc />
    public async Task<IGenericResult<FlowRunReport>> Run(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default)
    {
        var outcome = await Execute(graph, options, cancellationToken).ConfigureAwait(false);
        return outcome.Result.IsFailure || outcome.Report is not { } report
            ? outcome.Result.ToNewResult<FlowRunReport>()
            : GenericResult<FlowRunReport>.Success(report);
    }

    /// <inheritdoc />
    public async Task<FlowRunOutcome> Execute(FlowGraph graph, FlowRunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);
        var pending = new Pending();
        lock (_running)
        {
            _running.Add(pending);
        }

        FlowRunOutcome outcome;
        try
        {
            outcome = await _inner.Execute(graph, options.WithProgress(pending), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_running)
            {
                _running.Remove(pending);
            }
        }

        // Why only a flow that succeeded is counted: a graph that fails adds nothing, the run is reported failed, not partly counted.
        if (outcome.Result.IsSuccess && outcome.Report is not null)
        {
            Interlocked.Add(ref _extracted, pending.Extracted);
            Interlocked.Add(ref _transformed, pending.Transformed);
            Interlocked.Add(ref _rejected, pending.Rejected);
        }

        return outcome;
    }

    private long Running(Func<Pending, long> count)
    {
        lock (_running)
        {
            return _running.Sum(count);
        }
    }

    // What one graph has counted so far, as its components finish: a source's rows are final when it has finished, and so are the rows that reached a
    // destination or left an error output.
    private sealed class Pending : IFlowRunProgress
    {
        private long _extracted;
        private long _transformed;
        private long _rejected;

        public long Extracted => Interlocked.Read(ref _extracted);

        public long Transformed => Interlocked.Read(ref _transformed);

        public long Rejected => Interlocked.Read(ref _rejected);

        public void ComponentFinished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links)
        {
            if (component.Inputs.Count == 0)
            {
                Interlocked.Add(ref _extracted, links.Where(l => Same(l.Link.FromComponent, component.Name)).Sum(l => l.Rows));
            }

            if (component.Outputs.Count == 0)
            {
                Interlocked.Add(ref _transformed, links.Where(l => Same(l.Link.ToComponent, component.Name)).Sum(l => l.Rows));
            }

            // A row is rejected when it leaves through an error output of the component that rejected it.
            if (component is IFlowErrorOutputs errors)
            {
                Interlocked.Add(ref _rejected, links
                    .Where(l => Same(l.Link.FromComponent, component.Name) && errors.ErrorOutputs.Contains(l.Link.FromOutput, StringComparer.OrdinalIgnoreCase))
                    .Sum(l => l.Rows));
            }
        }

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
