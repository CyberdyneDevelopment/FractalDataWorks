using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Per-execution control surface for test-mode pipeline runs.
/// Keyed by execution ID; the singleton manages state for all active test executions.
/// </summary>
public interface IPipelineTestController
{
    /// <summary>
    /// Registers a new test execution so subsequent control calls are routed correctly.
    /// Must be called before any Pause/Resume/Step/Abort calls for the execution.
    /// </summary>
    /// <param name="executionId">The execution ID returned from the trigger endpoint.</param>
    /// <returns>The <see cref="PipelineTestExecutionState"/> entry for use by the pipeline runtime.</returns>
    PipelineTestExecutionState Register(Guid executionId);

    /// <summary>
    /// Removes the state for a completed or aborted test execution, releasing memory.
    /// </summary>
    /// <param name="executionId">The execution ID to remove.</param>
    void Unregister(Guid executionId);

    /// <summary>
    /// Sets the pause flag on an active test execution. The pipeline's batch loop awaits this
    /// flag's <see cref="System.Threading.ManualResetEventSlim"/> before processing the next batch.
    /// </summary>
    /// <param name="executionId">The execution ID to pause.</param>
    void Pause(Guid executionId);

    /// <summary>
    /// Releases the pause flag, allowing the pipeline to continue processing.
    /// </summary>
    /// <param name="executionId">The execution ID to resume.</param>
    void Resume(Guid executionId);

    /// <summary>
    /// Releases the pause flag for exactly one source-extract batch, then re-sets it.
    /// The pipeline reads <see cref="PipelineTestExecutionState.StepPending"/> to know it should
    /// re-pause after one batch completes.
    /// </summary>
    /// <param name="executionId">The execution ID to step.</param>
    void Step(Guid executionId);

    /// <summary>
    /// Cancels the <see cref="System.Threading.CancellationTokenSource"/> for the execution,
    /// causing the pipeline's <see cref="System.Threading.CancellationToken"/> to fire and
    /// the run to terminate.
    /// </summary>
    /// <param name="executionId">The execution ID to abort.</param>
    void Abort(Guid executionId);

    /// <summary>
    /// Gets the current state for an active test execution, or null if not registered.
    /// </summary>
    /// <param name="executionId">The execution ID to look up.</param>
    PipelineTestExecutionState? GetState(Guid executionId);
}
