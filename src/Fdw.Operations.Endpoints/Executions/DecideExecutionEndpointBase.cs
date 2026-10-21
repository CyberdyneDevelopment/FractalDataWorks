using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// What approving and denying a run that waits for approval have in common (FDW-1740): the caller must hold <c>pipelines:approve</c>, the run
/// must be waiting, and the decision moves it with the signed-in caller as the actor, so its state-change event says who decided, when and why.
/// </summary>
/// <remarks>
/// <para>
/// Why the actor is read from the sign-in and never from the request: who approved a run is a fact about the caller, and a body that could
/// name anyone would let any caller approve as someone else.
/// </para>
/// <para>
/// Why a run that is not waiting is refused with 409 naming its state: a second approve, or a deny of a run that was approved meanwhile,
/// must change nothing.
/// </para>
/// </remarks>
public abstract class DecideExecutionEndpointBase : Endpoint<ExecutionStateRequest, ExecutionDetailDto>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>Initializes a new instance of the <see cref="DecideExecutionEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tracker">Reads and moves the executions.</param>
    protected DecideExecutionEndpointBase(ILogger logger, IExecutionTracker tracker)
    {
        EndpointLogger = logger;
        _tracker = tracker;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Gets the route verb of the decision, for the route and the summary: <c>approve</c> or <c>deny</c>.</summary>
    protected abstract string Decision { get; }

    /// <summary>Gets the state the decision moves the run to.</summary>
    protected abstract IExecutionStateType Target { get; }

    /// <summary>Gets the message the state-change event carries when the request gives none.</summary>
    protected abstract string DefaultMessage { get; }

    /// <summary>Gets the one-line summary of the endpoint.</summary>
    protected abstract string EndpointSummary { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post($"/executions/{{Id}}/{Decision}");
#if DEVELOP
        AllowAnonymous();
#else
        // Why its own permission: starting a run (pipelines:execute) must not carry the right to release one that waits for approval.
        Policies("pipelines:approve");
#endif
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = "Decides a run that waits for approval. The run must be AwaitingApproval; the decision is recorded with the signed-in caller and the optional message.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ExecutionStateRequest req, CancellationToken ct)
    {
        var actor = new ClaimsPrincipalAuthenticationContext(User).UserId;
        if (string.IsNullOrWhiteSpace(actor))
        {
            OnCallerUnknown(req.Id, Decision);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        OnDeciding(req.Id, Decision, actor);
        var found = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!found.IsSuccess || found.Value is null)
        {
            OnExecutionNotFound(req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var execution = found.Value;
        if (!string.Equals(execution.State.Name, ExecutionStateTypes.AwaitingApproval.Name, StringComparison.Ordinal))
        {
            OnNotAwaitingApproval(req.Id, execution.State.Name);
            ThrowError($"Execution is '{execution.State.Name}', not AwaitingApproval, so it cannot be {Decision}d", 409);
            return;
        }

        var moved = await _tracker.TransitionState(req.Id, Target, req.Message ?? DefaultMessage, actor, ct).ConfigureAwait(false);
        if (moved.IsFailure)
        {
            // The run was waiting a moment ago; the only way the move is refused is that someone decided it first.
            OnDecisionRefused(req.Id, moved.CurrentMessage);
            ThrowError("The execution was decided by someone else first", 409);
            return;
        }

        var released = await Release(execution, ct).ConfigureAwait(false);
        if (released.IsFailure)
        {
            OnReleaseFailed(req.Id, released.CurrentMessage);
            await ReturnToWaiting(req.Id, actor, ct).ConfigureAwait(false);
            await Send.ResultAsync(TypedResults.Json(new { Error = $"The execution could not be {Decision}d", Details = released.CurrentMessage }, statusCode: 503)).ConfigureAwait(false);
            return;
        }

        OnDecided(req.Id, Decision, actor);
        var updated = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        await Send.OkAsync(ExecutionDetails.Of(updated.IsSuccess && updated.Value is not null ? updated.Value : execution), ct).ConfigureAwait(false);
    }

    /// <summary>Carries out what the decision means for the run once it has moved (approve queues it). Nothing by default.</summary>
    /// <param name="execution">The run as it was when it was waiting.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success, or the failure that puts the run back to waiting.</returns>
    protected virtual Task<IGenericResult> Release(IExecutionItem execution, CancellationToken ct) =>
        Task.FromResult<IGenericResult>(GenericResult.Success());

    // A run whose release failed is not left approved with nobody running it: it goes back to waiting, so the decision can be made again.
    private async Task ReturnToWaiting(Guid id, string actor, CancellationToken ct)
    {
        var back = await _tracker.TransitionState(id, ExecutionStateTypes.AwaitingApproval, "Returned to waiting: the decision could not be carried out", actor, ct).ConfigureAwait(false);
        if (back.IsFailure)
        {
            OnReturnToWaitingFailed(id, back.CurrentMessage);
        }
    }

    /// <summary>Called when the caller carries no identity.</summary>
    /// <param name="id">The run.</param>
    /// <param name="decision">The decision.</param>
    protected virtual void OnCallerUnknown(Guid id, string decision)
    {
    }

    /// <summary>Called when a decision starts.</summary>
    /// <param name="id">The run.</param>
    /// <param name="decision">The decision.</param>
    /// <param name="actor">The signed-in caller.</param>
    protected virtual void OnDeciding(Guid id, string decision, string actor)
    {
    }

    /// <summary>Called when the run does not exist.</summary>
    /// <param name="id">The run.</param>
    protected virtual void OnExecutionNotFound(Guid id)
    {
    }

    /// <summary>Called when the run is not waiting for approval.</summary>
    /// <param name="id">The run.</param>
    /// <param name="state">The state it is in.</param>
    protected virtual void OnNotAwaitingApproval(Guid id, string state)
    {
    }

    /// <summary>Called when the tracker refuses the move.</summary>
    /// <param name="id">The run.</param>
    /// <param name="error">Why.</param>
    protected virtual void OnDecisionRefused(Guid id, string? error)
    {
    }

    /// <summary>Called when the decision could not be carried out.</summary>
    /// <param name="id">The run.</param>
    /// <param name="error">Why.</param>
    protected virtual void OnReleaseFailed(Guid id, string? error)
    {
    }

    /// <summary>Called when the run could not be put back to waiting after a failed release.</summary>
    /// <param name="id">The run.</param>
    /// <param name="error">Why.</param>
    protected virtual void OnReturnToWaitingFailed(Guid id, string? error)
    {
    }

    /// <summary>Called when the decision is made and carried out.</summary>
    /// <param name="id">The run.</param>
    /// <param name="decision">The decision.</param>
    /// <param name="actor">The signed-in caller.</param>
    protected virtual void OnDecided(Guid id, string decision, string actor)
    {
    }
}
