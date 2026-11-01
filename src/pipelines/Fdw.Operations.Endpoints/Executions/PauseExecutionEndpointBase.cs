using System;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to pause an execution.
/// </summary>
public abstract class PauseExecutionEndpointBase : ExecutionRunStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PauseExecutionEndpointBase"/> class.
    /// </summary>
    protected PauseExecutionEndpointBase(ILogger logger, IExecutionTracker tracker, IExecutionRunControl runControl, IUserNames userNames)
        : base(logger, tracker, runControl, userNames)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/executions/{Id}/pause");
#if DEVELOP
        AllowAnonymous();
#else
        // Why execute, not read: stopping or resuming a run changes it; a read permission never may.
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Pause execution";
            s.Description = "Pauses a running execution: its run holds before its next batch of rows until it is resumed or cancelled.";
        });
    }

    /// <inheritdoc/>
    protected override IExecutionStateType TargetState => ExecutionStateTypes.Paused;

    /// <inheritdoc/>
    protected override string Verb => "pause";

    /// <inheritdoc/>
    protected override IGenericResult Act(Guid executionId) => RunControl.Pause(executionId);

    /// <inheritdoc/>
    protected override void Undo(Guid executionId) => RunControl.Resume(executionId);

    /// <inheritdoc/>
    protected override void OnRequested(Guid id) => OnPausingExecution(id);

    /// <inheritdoc/>
    protected override void OnNotFound(Guid id) => OnExecutionNotFound(id);

    /// <inheritdoc/>
    protected override void OnTerminal(Guid id, string state) => OnExecutionAlreadyTerminal(id, state);

    /// <inheritdoc/>
    protected override void OnFailed(Guid id, string error) => OnPauseFailed(id, error);

    /// <inheritdoc/>
    protected override void OnDone(Guid id) => OnExecutionPaused(id);

    /// <summary>Called when pauseing an execution. Override for custom logging.</summary>
    protected virtual void OnPausingExecution(Guid id) { }

    /// <summary>Called when execution is not found. Override for custom logging.</summary>
    protected virtual void OnExecutionNotFound(Guid id) { }

    /// <summary>Called when execution is already terminal. Override for custom logging.</summary>
    protected virtual void OnExecutionAlreadyTerminal(Guid id, string state) { }

    /// <summary>Called when pause fails. Override for custom logging.</summary>
    protected virtual void OnPauseFailed(Guid id, string error) { }

    /// <summary>Called when the execution is done. Override for custom logging.</summary>
    protected virtual void OnExecutionPaused(Guid id) { }
}
