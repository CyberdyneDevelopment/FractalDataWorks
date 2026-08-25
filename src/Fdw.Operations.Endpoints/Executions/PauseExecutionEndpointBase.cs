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
/// Endpoint to pause an execution.
/// </summary>
public abstract class PauseExecutionEndpointBase : Endpoint<ExecutionStateRequest, ExecutionDetailDto>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PauseExecutionEndpointBase"/> class.
    /// </summary>
    protected PauseExecutionEndpointBase(IExecutionTracker tracker)
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
        Post("/executions/{Id}/pause");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Pause execution";
            s.Description = "Pauses a running execution. Can only pause executions in Running state.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecutionStateRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnPausingExecution(req.Id);

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

        // Transition to Paused
        var transitionResult = await _tracker.TransitionState(
            req.Id,
            ExecutionStateTypes.Paused,
            message: req.Message ?? "Paused via API",
            actor: req.Actor ?? "API:PauseEndpoint",
            cancellationToken: ct).ConfigureAwait(false);

        if (!transitionResult.IsSuccess)
        {
            var errorMessage = transitionResult.CurrentMessage ?? "Failed to pause execution";
            OnPauseFailed(req.Id, errorMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to pause execution", Details = errorMessage }, ct).ConfigureAwait(false);
            return;
        }

        OnExecutionPaused(req.Id);

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

    /// <summary>Called when pausing execution. Override for custom logging.</summary>
    protected virtual void OnPausingExecution(Guid id) { }

    /// <summary>Called when execution is not found. Override for custom logging.</summary>
    protected virtual void OnExecutionNotFound(Guid id) { }

    /// <summary>Called when execution is already terminal. Override for custom logging.</summary>
    protected virtual void OnExecutionAlreadyTerminal(Guid id, string state) { }

    /// <summary>Called when pause fails. Override for custom logging.</summary>
    protected virtual void OnPauseFailed(Guid id, string error) { }

    /// <summary>Called when execution is paused. Override for custom logging.</summary>
    protected virtual void OnExecutionPaused(Guid id) { }
}
