using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Singleton implementation of <see cref="IPipelineTestController"/>.
/// Manages pause/resume/step/abort state for all active test executions concurrently.
/// </summary>
public sealed class PipelineTestController : IPipelineTestController
{
    private readonly ILogger<PipelineTestController> _logger;
    private readonly ConcurrentDictionary<Guid, PipelineTestExecutionState> _states = new();

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineTestController"/>.
    /// </summary>
    public PipelineTestController(ILogger<PipelineTestController>? logger = null)
    {
        _logger = logger ?? NullLogger<PipelineTestController>.Instance;
    }

    /// <inheritdoc/>
    public PipelineTestExecutionState Register(Guid executionId)
    {
        var state = new PipelineTestExecutionState();
        _states[executionId] = state;
        EtlLog.TestControllerRegistered(_logger, executionId);
        return state;
    }

    /// <inheritdoc/>
    public void Unregister(Guid executionId)
    {
        if (_states.TryRemove(executionId, out var state))
        {
            state.PauseEvent.Dispose();
            state.Cts.Dispose();
            EtlLog.TestControllerUnregistered(_logger, executionId);
        }
    }

    /// <inheritdoc/>
    public void Pause(Guid executionId)
    {
        if (!_states.TryGetValue(executionId, out var state)) return;
        state.PauseEvent.Reset();
        EtlLog.TestExecutionPaused(_logger, executionId);
    }

    /// <inheritdoc/>
    public void Resume(Guid executionId)
    {
        if (!_states.TryGetValue(executionId, out var state)) return;
        state.StepPending = false;
        state.PauseEvent.Set();
        EtlLog.TestExecutionResumed(_logger, executionId);
    }

    /// <inheritdoc/>
    public void Step(Guid executionId)
    {
        if (!_states.TryGetValue(executionId, out var state)) return;
        state.StepPending = true;
        state.PauseEvent.Set();
        EtlLog.TestExecutionStepped(_logger, executionId);
    }

    /// <inheritdoc/>
    public void Abort(Guid executionId)
    {
        if (!_states.TryGetValue(executionId, out var state)) return;
        state.PauseEvent.Set();
        if (!state.Cts.IsCancellationRequested)
        {
            state.Cts.Cancel();
        }

        EtlLog.TestExecutionAborted(_logger, executionId);
    }

    /// <inheritdoc/>
    public PipelineTestExecutionState? GetState(Guid executionId)
    {
        _states.TryGetValue(executionId, out var state);
        return state;
    }
}
