using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Dispatch;

/// <summary>
/// Dispatches an accepted operation for execution.
/// Implementations bridge the gap between trigger acceptance and actual work execution.
/// </summary>
/// <remarks>
/// <para>
/// After a trigger endpoint creates an execution item and transitions it to the Triggered state,
/// the dispatcher is responsible for handing the execution off to the appropriate execution engine.
/// This could be a background job queue, an orchestration engine, a direct in-process invocation,
/// or an external service call.
/// </para>
/// <para>
/// Use <see cref="NullOperationDispatcher"/> when trigger-and-track is sufficient and dispatch
/// is handled externally (e.g., by a polling scheduler or webhook).
/// </para>
/// </remarks>
public interface IOperationDispatcher
{
    /// <summary>
    /// Dispatches the given execution item for processing.
    /// </summary>
    /// <param name="execution">The execution item that has been accepted and is ready for dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether the dispatch was successful.</returns>
    Task<IGenericResult> Dispatch(IExecutionItem execution, CancellationToken cancellationToken = default);
}
