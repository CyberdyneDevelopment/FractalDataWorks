using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// What the ETL host is running now, component by component (FDW-3382): the live view reads it. It keeps no history and no row values; a run is in it from the moment
/// its pipeline begins until the pipeline ends.
/// </summary>
public sealed class RunningFlows
{
    private readonly ConcurrentDictionary<Guid, RunLive> _runs = new();
    private readonly ILogger<RunningFlows> _logger;

    /// <summary>Initializes a new instance of the <see cref="RunningFlows"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public RunningFlows(ILogger<RunningFlows>? logger = null)
    {
        _logger = logger ?? NullLogger<RunningFlows>.Instance;
    }

    /// <summary>Begins following a pipeline run.</summary>
    /// <param name="executionId">The execution record of the run.</param>
    /// <returns>The run, to hand to the executor as <see cref="FlowRunOptions.Live"/> and to dispose when the pipeline ends.</returns>
    public RunLive Begin(Guid executionId)
    {
        var live = new RunLive(() => End(executionId));
        _runs[executionId] = live;
        FlowLog.LiveRunOpened(_logger, executionId);
        return live;
    }

    /// <summary>Reads the components a run is running now.</summary>
    /// <param name="executionId">The execution record of the run.</param>
    /// <returns>The components, or <see langword="null"/> when the host is not following the run (it has not begun, or it has ended).</returns>
    public IReadOnlyList<FlowLiveComponentView>? Components(Guid executionId) => _runs.TryGetValue(executionId, out var live) ? live.View() : null;

    /// <summary>
    /// What every link of a run's graphs has carried so far (FDW-3778), or <see langword="null"/> when the host does not follow the run.
    /// </summary>
    /// <param name="executionId">The run.</param>
    public IReadOnlyList<FlowLinkCount>? Links(Guid executionId) => _runs.TryGetValue(executionId, out var live) ? live.Links() : null;

    private void End(Guid executionId)
    {
        if (_runs.TryRemove(executionId, out _))
        {
            FlowLog.LiveRunClosed(_logger, executionId);
        }
    }
}
