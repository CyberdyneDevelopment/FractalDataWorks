using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Executes an orchestration node tree recursively.
/// Branch nodes (CanHostPipelines=false) execute children sequentially in Ordinal order.
/// Leaf nodes (CanHostPipelines=true) execute pipeline memberships in parallel bounded by MaxParallelPipelines.
/// </summary>
public interface IOrchestrationNodeOrchestrator
{
    /// <summary>
    /// Executes the orchestration node tree rooted at the node identified by <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The orchestration node execution request.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    Task Execute(OrchestrationNodeExecutionRequest request, CancellationToken cancellationToken = default);
}
