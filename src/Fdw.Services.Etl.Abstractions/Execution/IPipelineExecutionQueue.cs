using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Queue for submitting pipeline execution requests to the background executor.
/// Separates the "submit work" concern (endpoints) from the "process work" concern
/// (PipelineExecutionBackgroundService).
/// </summary>
public interface IPipelineExecutionQueue
{
    /// <summary>
    /// Enqueues a pipeline execution request.
    /// </summary>
    /// <param name="request">The execution request to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the request was enqueued; <c>false</c> if the queue is full (backpressure).
    /// Why <c>bool</c> return instead of throwing: callers (endpoints) can return HTTP 503
    /// Service Unavailable cleanly without exception overhead.
    /// </returns>
    ValueTask<bool> Enqueue(PipelineExecutionRequest request, CancellationToken cancellationToken = default);
}
