using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// The one way an execution is told to cancel, pause or resume (FDW-2950): the run is told first, then the execution's row moves, and the
/// person who asked is the one the row names.
/// </summary>
/// <remarks>
/// Why the run first: moving the row alone recorded a decision nothing obeyed. A run the host does not hold fails the request before the row
/// moves, so the row never says a state the run did not reach. When the row then refuses the move (the state machine does not allow it from
/// where the run is), the run's own change is undone, so the two do not part.
/// </remarks>
public abstract class ExecutionRunStateEndpointBase : Endpoint<ExecutionStateRequest, ExecutionDetailDto>
{
    private readonly IUserNames _userNames;

    /// <summary>Initializes a new instance of the <see cref="ExecutionRunStateEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tracker">The execution tracker.</param>
    /// <param name="runControl">The host's control of the runs it holds.</param>
    /// <param name="userNames">Reads the name of the person who asked.</param>
    protected ExecutionRunStateEndpointBase(ILogger logger, IExecutionTracker tracker, IExecutionRunControl runControl, IUserNames userNames)
    {
        EndpointLogger = logger;
        Tracker = tracker;
        RunControl = runControl;
        _userNames = userNames;
    }

    /// <summary>Gets the execution tracker.</summary>
    protected IExecutionTracker Tracker { get; }

    /// <summary>Gets the host's control of the runs it holds.</summary>
    protected IExecutionRunControl RunControl { get; }

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Gets the state the execution's row moves to.</summary>
    protected abstract IExecutionStateType TargetState { get; }

    /// <summary>Gets the verb the refusals name, such as "cancel".</summary>
    protected abstract string Verb { get; }

    /// <summary>Tells the run.</summary>
    /// <param name="executionId">The execution's id.</param>
    /// <returns>A failure when the host holds no such run or the run cannot take it.</returns>
    protected abstract IGenericResult Act(Guid executionId);

    /// <summary>Takes back what <see cref="Act"/> did, when the row then refuses the move.</summary>
    /// <param name="executionId">The execution's id.</param>
    protected virtual void Undo(Guid executionId)
    {
    }

    /// <summary>Called when the request starts.</summary>
    protected abstract void OnRequested(Guid id);

    /// <summary>Called when the execution is not found.</summary>
    protected abstract void OnNotFound(Guid id);

    /// <summary>Called when the execution has already finished.</summary>
    protected abstract void OnTerminal(Guid id, string state);

    /// <summary>Called when the change fails.</summary>
    protected abstract void OnFailed(Guid id, string error);

    /// <summary>Called when the change is done.</summary>
    protected abstract void OnDone(Guid id);

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecutionStateRequest req, CancellationToken ct)
    {
        OnRequested(req.Id);

        var found = await Tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!found.IsSuccess || found.Value is not { } execution)
        {
            OnNotFound(req.Id);
            await Refuse(GenericResult.Failure(ExecutionRunStateLog.ExecutionNotFound(EndpointLogger, req.Id, Verb))).ConfigureAwait(false);
            return;
        }

        if (execution.State.IsTerminal)
        {
            OnTerminal(req.Id, execution.State.Name);
            await Refuse(GenericResult.Failure(ExecutionRunStateLog.AlreadyFinished(EndpointLogger, req.Id, Verb, execution.State.Name))).ConfigureAwait(false);
            return;
        }

        var changed = await Change(req, ct).ConfigureAwait(false);
        if (changed.IsFailure)
        {
            OnFailed(req.Id, string.Concat(changed.CurrentMessage));
            await Refuse(changed).ConfigureAwait(false);
            return;
        }

        OnDone(req.Id);
        var updated = await Tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!updated.IsSuccess || updated.Value is not { } item)
        {
            await Refuse(updated.ToNewResult<ExecutionDetailDto>()).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDetailDto(item), ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> Change(ExecutionStateRequest req, CancellationToken ct)
    {
        var actor = await Actor(req.Id, ct).ConfigureAwait(false);
        if (actor.IsFailure || actor.Value is not { } name) return actor.ToNewResult<string>();

        var acted = Act(req.Id);
        if (acted.IsFailure) return acted;

        var moved = await Tracker.TransitionState(req.Id, TargetState, message: req.Message, actor: name, cancellationToken: ct).ConfigureAwait(false);
        if (moved.IsFailure) Undo(req.Id);

        return moved;
    }

    // The person who asked, by name, from the token: never from the body, where anyone can write anyone.
    private async Task<IGenericResult<string>> Actor(Guid executionId, CancellationToken ct)
    {
        if (!Guid.TryParse(HttpContext.User.FindFirstValue("sub"), out var userId)
            && !Guid.TryParse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out userId))
        {
            return GenericResult<string>.Failure(ExecutionRunStateLog.ActorUnknown(EndpointLogger, executionId, Verb));
        }

        return await _userNames.Get(userId, ct).ConfigureAwait(false);
    }

    private Task<FastEndpoints.Void> Refuse(IGenericResult failure) => Send.ResultAsync(ResultHttpStatusMapper.Answer(failure, HttpContext));

    /// <summary>Maps an execution item to a detail DTO.</summary>
    protected virtual ExecutionDetailDto MapToDetailDto(IExecutionItem item)
    {
        return new ExecutionDetailDto
        {
            Id = item.Id,
            ParentId = item.ParentId,
            RootId = item.RootId,
            ItemType = item.ItemType.Name,
            Name = item.Name,
            State = item.State.Name,
            CorrelationId = item.CorrelationId,
            TriggerSource = item.TriggerSource,
            Parameters = item.Parameters,
            CreatedAt = item.CreatedAt,
            StartedAt = item.StartedAt,
            CompletedAt = item.CompletedAt,
            ResultCode = item.ResultCode,
            ResultMessage = item.ResultMessage
        };
    }
}
