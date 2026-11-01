using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// <c>POST /executions/{Id}/deny</c>: refuses a run that waits for approval (FDW-1740). The run ends Cancelled, and the optional message in the
/// request is the reason its state-change event carries.
/// </summary>
public abstract class DenyExecutionEndpointBase : DecideExecutionEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="DenyExecutionEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tracker">Reads and moves the executions.</param>
    protected DenyExecutionEndpointBase(ILogger logger, IExecutionTracker tracker)
        : base(logger, tracker)
    {
    }

    /// <inheritdoc />
    protected override string Decision => "deny";

    /// <inheritdoc />
    protected override IExecutionStateType Target => ExecutionStateTypes.Cancelled;

    /// <inheritdoc />
    protected override string DefaultMessage => "Denied";

    /// <inheritdoc />
    protected override string EndpointSummary => "Deny a run that waits for approval";
}
