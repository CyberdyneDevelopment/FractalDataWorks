using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Singleton registry that allows the project orchestrator to await pipeline completion
/// without polling. Each in-flight pipeline registers a TaskCompletionSource, which is
/// signaled by the PipelineExecutionBackgroundService when it finishes.
/// </summary>
/// <remarks>
/// The orchestrator uses <see cref="Register"/> before dispatching a pipeline to the queue,
/// then calls <see cref="Await"/> to wait for that pipeline's execution result.
/// The pipeline execution background service calls <see cref="Signal"/> from its
/// <c>CompleteWithMetrics</c> hook to release the awaiting orchestrator task.
/// </remarks>
public interface IExecutionCompletionSignaler
{
    /// <summary>
    /// Registers a new completion source for the given execution item identifier.
    /// Must be called before dispatching the pipeline to the execution queue.
    /// </summary>
    /// <param name="executionItemId">The execution item identifier assigned to this pipeline run.</param>
    void Register(Guid executionItemId);

    /// <summary>
    /// Signals completion of the pipeline execution identified by <paramref name="executionItemId"/>.
    /// Called by PipelineExecutionBackgroundService from its CompleteWithMetrics hook.
    /// </summary>
    /// <param name="executionItemId">The execution item identifier that completed.</param>
    /// <param name="succeeded">Whether the pipeline execution succeeded.</param>
    /// <param name="resultMessage">Optional completion message or error detail.</param>
    void Signal(Guid executionItemId, bool succeeded, string? resultMessage);

    /// <summary>
    /// Awaits the completion of the pipeline execution identified by <paramref name="executionItemId"/>.
    /// Returns when <see cref="Signal"/> is called or the cancellation token is cancelled.
    /// </summary>
    /// <param name="executionItemId">The execution item identifier to wait on.</param>
    /// <param name="cancellationToken">Token to cancel the wait (e.g., when policy halts the stage).</param>
    /// <returns>True if the pipeline succeeded; false if it failed.</returns>
    Task<bool> Await(Guid executionItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the completion source for the given execution item identifier, releasing memory.
    /// Called after <see cref="Await"/> returns, whether normally or via cancellation.
    /// </summary>
    /// <param name="executionItemId">The execution item identifier to deregister.</param>
    void Deregister(Guid executionItemId);
}
