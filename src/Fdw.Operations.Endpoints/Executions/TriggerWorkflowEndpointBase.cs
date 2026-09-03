using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Web.RestEndpoints.Base;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to trigger a workflow execution.
/// </summary>
public abstract class TriggerWorkflowEndpointBase : TriggerEndpointBase<TriggerOperationRequest>
{
    /// <summary>Initializes a new instance of the <see cref="TriggerWorkflowEndpointBase"/> class.</summary>
    protected TriggerWorkflowEndpointBase(ILogger<TriggerWorkflowEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "workflows";

    /// <inheritdoc/>
    protected override IExecutionItemType ItemType => ExecutionItemTypes.Workflow;
}
