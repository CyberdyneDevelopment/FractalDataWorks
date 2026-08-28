using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to get execution details by ID.
/// </summary>
public abstract class GetExecutionEndpointBase : Endpoint<ExecutionIdRequest, ExecutionDetailDto>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetExecutionEndpointBase"/> class.
    /// </summary>
    protected GetExecutionEndpointBase(IExecutionTracker tracker)
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
        Get("/executions/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get execution by ID";
            s.Description = "Returns detailed information about a specific execution.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecutionIdRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnFetchingExecution(req.Id);

        var result = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.CurrentMessage ?? "Unknown error";

            if (errorMessage.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
            {
                OnExecutionNotFound(req.Id);
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = "NotFound",
                    messages = new[] { $"Execution '{req.Id}' was not found." }
                }, ct).ConfigureAwait(false);
                return;
            }

            OnExecutionFetchFailed(req.Id, errorMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch execution", Details = errorMessage }, ct).ConfigureAwait(false);
            return;
        }

        var execution = result.Value;
        if (execution == null)
        {
            OnExecutionNotFound(req.Id);
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "NotFound",
                messages = new[] { $"Execution '{req.Id}' was not found." }
            }, ct).ConfigureAwait(false);
            return;
        }

        OnExecutionRetrieved(req.Id);

        await Send.OkAsync(MapToDetailDto(execution), ct).ConfigureAwait(false);
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

    /// <summary>
    /// Called when fetching an execution. Override for custom logging.
    /// </summary>
    protected virtual void OnFetchingExecution(Guid id)
    {
    }

    /// <summary>
    /// Called when execution fetch fails. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionFetchFailed(Guid id, string error)
    {
    }

    /// <summary>
    /// Called when execution is not found. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionNotFound(Guid id)
    {
    }

    /// <summary>
    /// Called when execution is retrieved. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionRetrieved(Guid id)
    {
    }
}
