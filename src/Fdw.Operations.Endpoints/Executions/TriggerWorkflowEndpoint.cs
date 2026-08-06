using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Web.RestEndpoints.Base;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to trigger a workflow execution.
/// </summary>
public abstract class TriggerWorkflowEndpoint : TriggerEndpointBase<TriggerOperationRequest>
{
    /// <inheritdoc/>
    protected override string ResourceName => "workflows";

    /// <inheritdoc/>
    protected override IExecutionItemType ItemType => ExecutionItemTypes.Workflow;
}
