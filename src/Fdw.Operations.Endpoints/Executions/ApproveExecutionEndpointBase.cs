using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// <c>POST /executions/{Id}/approve</c>: lets a run that waits for approval start (FDW-1740). The run returns to Scheduled, the state every run
/// starts the normal way from, and the host queues it.
/// </summary>
public abstract class ApproveExecutionEndpointBase : DecideExecutionEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ApproveExecutionEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tracker">Reads and moves the executions.</param>
    protected ApproveExecutionEndpointBase(ILogger logger, IExecutionTracker tracker)
        : base(logger, tracker)
    {
    }

    /// <inheritdoc />
    protected override string Decision => "approve";

    /// <inheritdoc />
    protected override IExecutionStateType Target => ExecutionStateTypes.Scheduled;

    /// <inheritdoc />
    protected override string DefaultMessage => "Approved";

    /// <inheritdoc />
    protected override string EndpointSummary => "Approve a run that waits for approval";

    /// <summary>Queues the approved run the way the trigger would have: the host knows what a run of its kind is queued as.</summary>
    /// <param name="execution">The run as it was when it was waiting.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success, or the failure that returns the run to waiting.</returns>
    protected abstract override Task<IGenericResult> Release(IExecutionItem execution, CancellationToken ct);
}
