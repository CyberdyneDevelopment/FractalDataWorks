using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Runs a flow graph: validates it, starts every component as its own task on bounded channels of row batches, and ends the run
/// when every component has finished (FDW-1386).
/// </summary>
/// <remarks>
/// <para>
/// Back-pressure: a link buffers <see cref="FlowRunOptions.LinkCapacity"/> batches; a writer waits when the link is full.
/// </para>
/// <para>
/// Failure: the first component that fails or throws ends the run. The others are cancelled, their outputs are closed so nothing
/// downstream waits for rows that will not come, and the failure that is returned is the first one, not a cancellation it caused.
/// </para>
/// </remarks>
public sealed class FlowExecutor : IFlowExecutor
{
    private readonly ILogger<FlowExecutor> _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowExecutor"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public FlowExecutor(ILogger<FlowExecutor>? logger = null)
    {
        _logger = logger ?? NullLogger<FlowExecutor>.Instance;
    }

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

        if (options.LinkCapacity < 1)
        {
            return new FlowRunOutcome(GenericResult.Failure(FlowLog.CapacityInvalid(_logger, options.LinkCapacity)), null);
        }

        var valid = FlowGraphValidator.Validate(graph, _logger);
        if (valid.IsFailure)
        {
            return new FlowRunOutcome(valid, null);
        }

        var stopwatch = Stopwatch.StartNew();
        var run = FlowRun.Build(graph, options.LinkCapacity, options.PauseGate);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Why a completion source per component: a component in a later stage waits on the completion of each earlier one, which is
        // set when the component has run and closed its outputs, however it ended.
        var finished = graph.Components.ToDictionary(
            component => component.Name, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), StringComparer.OrdinalIgnoreCase);

        // Why a gate per stage: the first component of a stage to be released tells the observer the stage is starting (FDW-2060), once, and every
        // component of the stage waits for that, so a stage's record exists before anything in it runs.
        var gates = graph.Components.Select(component => graph.RankOf(component.Name)).Distinct()
            .ToDictionary(rank => rank, rank => new StageGate(() => options.Stages is { } observer ? observer.StageStarting(rank, stop.Token) : Task.CompletedTask));
        var components = await Task.WhenAll(
            graph.Components.Select(component => RunComponent(
                run, component, options, stop, WaitFor(graph, component, finished), finished[component.Name], graph.RankOf(component.Name), gates[graph.RankOf(component.Name)])))
            .ConfigureAwait(false);

        return Outcome(run, stopwatch.Elapsed, components, cancellationToken);
    }

    // Why the report is built before the outcome is decided: a failed or cancelled run still moved rows and measured its components, and
    // the record of the run needs that to say which component failed and how far the rows got (FDW-1691).
    private FlowRunOutcome Outcome(FlowRun run, TimeSpan elapsed, FlowComponentReport[] components, CancellationToken caller)
    {
        var links = run.Links.Select(l => l.Count).ToList();
        var report = new FlowRunReport(links, elapsed, components, run.FailedComponent, run.Rejections);
        if (run.Failure is { } failure)
        {
            return new FlowRunOutcome(failure, report);
        }

        if (caller.IsCancellationRequested)
        {
            return new FlowRunOutcome(GenericResult.Failure(FlowLog.RunCancelled(_logger)), report);
        }

        FlowLog.RunFinished(_logger, links.Count, links.Sum(l => l.Rows), (long)elapsed.TotalMilliseconds);
        return new FlowRunOutcome(GenericResult.Success(), report);
    }

    // One component, one task. Whatever happens it closes its outputs and releases what it left unread, so no neighbour waits on it.
    private async Task<FlowComponentReport> RunComponent(
        FlowRun run, IFlowComponent component, FlowRunOptions options, CancellationTokenSource stop, Task before, TaskCompletionSource done, int stage, StageGate stageStarted)
    {
        var ports = run.PortsOf(component.Name);
        var started = DateTimeOffset.UtcNow;
        var ended = started;
        var succeeded = false;
        try
        {
            await before.WaitAsync(stop.Token).ConfigureAwait(false);
            await stageStarted.Enter().WaitAsync(stop.Token).ConfigureAwait(false);

            started = DateTimeOffset.UtcNow;
            var result = await Task.Run(() => component.Run(ports, stop.Token), CancellationToken.None).ConfigureAwait(false);
            succeeded = result.IsSuccess;
            if (result.IsFailure)
            {
                Stop(run, stop, component.Name, GenericResult.Failure(FlowLog.ComponentFailed(_logger, component.Name, result.CurrentMessage)));
            }
        }
        catch (OperationCanceledException ex) when (stop.IsCancellationRequested)
        {
            // Another component failed or the caller cancelled; this component's own outcome is not a failure.
            FlowLog.ComponentCancelled(_logger, ex, component.Name);
        }
        catch (Exception ex)
        {
            Stop(run, stop, component.Name, GenericResult.Failure(FlowLog.ComponentThrew(_logger, component.Name, ex.GetType().Name, ex.Message)));
        }
        finally
        {
            ports.FinishOutputs();
            await Release(ports, component.Name, stop.Token).ConfigureAwait(false);

            // Why the end is taken before the completion is set: a source that waits for this one starts after it, so its start is
            // never before this end.
            ended = DateTimeOffset.UtcNow;

            // Why told before the completion is set: a component of a later stage starts when this one's completion is set, and an action among them may report
            // the rows this component carried (FDW-2041).
            options.Progress?.ComponentFinished(component, [.. run.Links.Select(link => link.Count)]);
            done.TrySetResult();
        }

        return new FlowComponentReport(component.Name, started, ended, succeeded, stage);
    }

    // Why Task.CompletedTask when nothing is stated: a component with no stage waits for nothing. A component in a stage waits for every
    // component of a lower stage and every component with none to finish (FDW-2040).
    private static Task WaitFor(FlowGraph graph, IFlowComponent component, Dictionary<string, TaskCompletionSource> finished)
    {
        var waits = new List<Task>();
        var rank = graph.RankOf(component.Name);
        if (rank > 0)
        {
            foreach (var other in graph.Components)
            {
                if (graph.RankOf(other.Name) < rank)
                {
                    waits.Add(finished[other.Name].Task);
                }
            }
        }

        return waits.Count == 0 ? Task.CompletedTask : Task.WhenAll(waits);
    }

    private async Task Release(FlowPorts ports, string component, CancellationToken stop)
    {
        try
        {
            var discarded = await ports.DrainInputs(stop).ConfigureAwait(false);
            foreach (var input in discarded)
            {
                FlowLog.InputNotConsumed(_logger, component, input.Key, input.Value);
            }
        }
        catch (OperationCanceledException ex)
        {
            // The run is being stopped; the channels are abandoned with it.
            FlowLog.ReleaseCancelled(_logger, ex, component);
        }
    }

    private static void Stop(FlowRun run, CancellationTokenSource stop, string component, IGenericResult failure)
    {
        run.Fail(component, failure);
        stop.Cancel();
    }

    // Tells the observer a stage is starting, once, however many of its components ask: the first to ask starts the telling and every one waits on it.
    private sealed class StageGate
    {
        private readonly Lock _lock = new();
        private readonly Func<Task> _start;
        private Task? _started;

        public StageGate(Func<Task> start)
        {
            _start = start;
        }

        public Task Enter()
        {
            lock (_lock)
            {
                return _started ??= _start();
            }
        }
    }
}
