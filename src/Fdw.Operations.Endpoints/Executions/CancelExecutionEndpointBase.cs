using System;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to cancel an execution.
/// </summary>
public abstract class CancelExecutionEndpointBase : ExecutionRunStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelExecutionEndpointBase"/> class.
    /// </summary>
    protected CancelExecutionEndpointBase(ILogger logger, IExecutionTracker tracker, IExecutionRunControl runControl, IUserNames userNames)
        : base(logger, tracker, runControl, userNames)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/executions/{Id}/cancel");
#if DEVELOP
        AllowAnonymous();
#else
        // Why execute, not read: stopping or resuming a run changes it; a read permission never may.
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Cancel execution";
            s.Description = "Cancels a waiting, running or paused execution and stops its run. Cannot cancel an execution that has finished.";
        });
    }

    /// <inheritdoc/>
    protected override IExecutionStateType TargetState => ExecutionStateTypes.Cancelled;

    /// <inheritdoc/>
    protected override string Verb => "cancel";

    /// <inheritdoc/>
    protected override IGenericResult Act(Guid executionId) => RunControl.Cancel(executionId);

    /// <inheritdoc/>
    protected override void OnRequested(Guid id) => OnCancellingExecution(id);

    /// <inheritdoc/>
    protected override void OnNotFound(Guid id) => OnExecutionNotFound(id);

    /// <inheritdoc/>
    protected override void OnTerminal(Guid id, string state) => OnExecutionAlreadyTerminal(id, state);

    /// <inheritdoc/>
    protected override void OnFailed(Guid id, string error) => OnCancelFailed(id, error);

    /// <inheritdoc/>
    protected override void OnDone(Guid id) => OnExecutionCancelled(id);

    /// <summary>Called when canceling an execution. Override for custom logging.</summary>
    protected virtual void OnCancellingExecution(Guid id) { }

    /// <summary>Called when execution is not found. Override for custom logging.</summary>
    protected virtual void OnExecutionNotFound(Guid id) { }

    /// <summary>Called when execution is already terminal. Override for custom logging.</summary>
    protected virtual void OnExecutionAlreadyTerminal(Guid id, string state) { }

    /// <summary>Called when cancel fails. Override for custom logging.</summary>
    protected virtual void OnCancelFailed(Guid id, string error) { }

    /// <summary>Called when the execution is done. Override for custom logging.</summary>
    protected virtual void OnExecutionCancelled(Guid id) { }
}
