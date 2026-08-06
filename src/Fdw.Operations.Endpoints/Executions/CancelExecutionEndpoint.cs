using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to cancel an execution.
/// </summary>
public abstract class CancelExecutionEndpoint : Endpoint<ExecutionStateRequest, ExecutionDetailDto>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelExecutionEndpoint"/> class.
    /// </summary>
    protected CancelExecutionEndpoint(IExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    /// <summary>
    /// Gets the execution tracker.
    /// </summary>
    protected IExecutionTracker Tracker => _tracker;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/executions/{Id}/cancel");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Cancel execution";
            s.Description = "Cancels a running or paused execution. Cannot cancel already completed or failed executions.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecutionStateRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnCancellingExecution(req.Id);

        // Get current execution
        var itemResult = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value == null)
        {
            OnExecutionNotFound(req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var execution = itemResult.Value;

        // Check if already in terminal state
        if (execution.State.IsTerminal)
        {
            OnExecutionAlreadyTerminal(req.Id, execution.State.Name);
            ThrowError($"Execution is already in terminal state '{execution.State.Name}'", 409);
            return;
        }

        // Transition to Cancelled
        var transitionResult = await _tracker.TransitionState(
            req.Id,
            ExecutionStateTypes.Cancelled,
            message: req.Message ?? "Cancelled via API",
            actor: req.Actor ?? "API:CancelEndpoint",
            cancellationToken: ct).ConfigureAwait(false);

        if (!transitionResult.IsSuccess)
        {
            var errorMessage = transitionResult.CurrentMessage ?? "Failed to cancel execution";
            OnCancelFailed(req.Id, errorMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to cancel execution", Details = errorMessage }, ct).ConfigureAwait(false);
            return;
        }

        OnExecutionCancelled(req.Id);

        // Get updated execution
        var updatedResult = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        var updatedExecution = updatedResult.IsSuccess ? updatedResult.Value ?? execution : execution;

        await Send.OkAsync(MapToDetailDto(updatedExecution), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps an execution item to a detail DTO.
    /// </summary>
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

    /// <summary>Called when cancelling execution. Override for custom logging.</summary>
    protected virtual void OnCancellingExecution(Guid id) { }

    /// <summary>Called when execution is not found. Override for custom logging.</summary>
    protected virtual void OnExecutionNotFound(Guid id) { }

    /// <summary>Called when execution is already terminal. Override for custom logging.</summary>
    protected virtual void OnExecutionAlreadyTerminal(Guid id, string state) { }

    /// <summary>Called when cancel fails. Override for custom logging.</summary>
    protected virtual void OnCancelFailed(Guid id, string error) { }

    /// <summary>Called when execution is cancelled. Override for custom logging.</summary>
    protected virtual void OnExecutionCancelled(Guid id) { }
}
