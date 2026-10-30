using System;
using System.Collections.Concurrent;
using System.Threading;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The pipeline runs this host holds, by execution id, and the means to stop, pause and resume them (FDW-2950).
/// </summary>
/// <remarks>
/// A run is held from the moment the worker queues it with the scheduler until it ends, so a cancel reaches a run that is still
/// waiting for its turn as well as one that is running. A paused run keeps its place: it keeps the server and tenant slots and the
/// tables it claimed, because the scheduler gives a place back only when the run ends.
/// </remarks>
public sealed class PipelineRunControl : IExecutionRunControl
{
    private readonly ConcurrentDictionary<Guid, PipelineRunHandle> _runs = new();
    private readonly ILogger<PipelineRunControl> _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunControl"/> class.</summary>
    public PipelineRunControl(ILogger<PipelineRunControl>? logger = null)
    {
        _logger = logger ?? NullLogger<PipelineRunControl>.Instance;
    }

    /// <summary>Holds a run until its handle is disposed.</summary>
    /// <param name="executionId">The execution row's id.</param>
    /// <param name="stoppingToken">The host's shutdown token, which also ends the run.</param>
    /// <returns>The run's handle.</returns>
    public PipelineRunHandle Register(Guid executionId, CancellationToken stoppingToken)
    {
        var handle = new PipelineRunHandle(executionId, _runs, stoppingToken);
        _runs[executionId] = handle;
        return handle;
    }

    /// <inheritdoc />
    public IGenericResult Cancel(Guid executionId)
    {
        if (!_runs.TryGetValue(executionId, out var run))
        {
            return GenericResult.Failure(PipelineRunControlLog.RunNotHeld(_logger, executionId, "stop"));
        }

        try
        {
            run.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return GenericResult.Failure(PipelineRunControlLog.RunNotHeld(_logger, executionId, "stop"));
        }

        PipelineRunControlLog.CancelRequested(_logger, executionId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult Pause(Guid executionId)
    {
        if (!_runs.TryGetValue(executionId, out var run))
        {
            return GenericResult.Failure(PipelineRunControlLog.RunNotHeld(_logger, executionId, "pause"));
        }

        if (!run.Gate.Pause())
        {
            return GenericResult.Failure(PipelineRunControlLog.AlreadyPaused(_logger, executionId));
        }

        PipelineRunControlLog.PauseRequested(_logger, executionId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult Resume(Guid executionId)
    {
        if (!_runs.TryGetValue(executionId, out var run))
        {
            return GenericResult.Failure(PipelineRunControlLog.RunNotHeld(_logger, executionId, "resume"));
        }

        if (!run.Gate.Resume())
        {
            return GenericResult.Failure(PipelineRunControlLog.NotPaused(_logger, executionId));
        }

        PipelineRunControlLog.ResumeRequested(_logger, executionId);
        return GenericResult.Success();
    }
}
