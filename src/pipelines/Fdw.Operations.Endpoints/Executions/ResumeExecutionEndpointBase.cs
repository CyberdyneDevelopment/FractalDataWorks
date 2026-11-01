using System;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to resume an execution.
/// </summary>
public abstract class ResumeExecutionEndpointBase : ExecutionRunStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeExecutionEndpointBase"/> class.
    /// </summary>
    protected ResumeExecutionEndpointBase(ILogger logger, IExecutionTracker tracker, IExecutionRunControl runControl, IUserNames userNames)
        : base(logger, tracker, runControl, userNames)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/executions/{Id}/resume");
#if DEVELOP
        AllowAnonymous();
#else
        // Why execute, not read: stopping or resuming a run changes it; a read permission never may.
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Resume execution";
            s.Description = "Resumes a paused execution: its run goes on from where it held.";
        });
    }

    /// <inheritdoc/>
    protected override IExecutionStateType TargetState => ExecutionStateTypes.Running;

    /// <inheritdoc/>
    protected override string Verb => "resume";

    /// <inheritdoc/>
    protected override IGenericResult Act(Guid executionId) => RunControl.Resume(executionId);

    /// <inheritdoc/>
    protected override void Undo(Guid executionId) => RunControl.Pause(executionId);

    /// <inheritdoc/>
    protected override void OnRequested(Guid id) => OnResumingExecution(id);

    /// <inheritdoc/>
    protected override void OnNotFound(Guid id) => OnExecutionNotFound(id);

    /// <inheritdoc/>
    protected override void OnTerminal(Guid id, string state) => OnExecutionAlreadyTerminal(id, state);

    /// <inheritdoc/>
    protected override void OnFailed(Guid id, string error) => OnResumeFailed(id, error);

    /// <inheritdoc/>
    protected override void OnDone(Guid id) => OnExecutionResumed(id);

    /// <summary>Called when resumeing an execution. Override for custom logging.</summary>
    protected virtual void OnResumingExecution(Guid id) { }

    /// <summary>Called when execution is not found. Override for custom logging.</summary>
    protected virtual void OnExecutionNotFound(Guid id) { }

    /// <summary>Called when execution is already terminal. Override for custom logging.</summary>
    protected virtual void OnExecutionAlreadyTerminal(Guid id, string state) { }

    /// <summary>Called when resume fails. Override for custom logging.</summary>
    protected virtual void OnResumeFailed(Guid id, string error) { }

    /// <summary>Called when the execution is done. Override for custom logging.</summary>
    protected virtual void OnExecutionResumed(Guid id) { }
}
