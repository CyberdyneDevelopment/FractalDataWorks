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
    /// Whether the request was taken, and when it was not, why: the queue is full (backpressure), or a threshold on waiting runs that stops new runs fires
    /// (FDW-3161). Why a result instead of throwing: callers (endpoints) can return HTTP 503 Service Unavailable cleanly without exception overhead, and
    /// close the run's record with the reason.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">The run's request could not be recorded, or the thresholds that decide whether it is taken cannot be read or are stated wrongly.</exception>
    ValueTask<PipelineEnqueueResult> Enqueue(PipelineExecutionRequest request, CancellationToken cancellationToken = default);
}
