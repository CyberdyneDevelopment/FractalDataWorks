using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to get children of an execution.
/// </summary>
public abstract class GetExecutionChildrenEndpoint : Endpoint<ExecutionIdRequest, List<ExecutionSummaryResponse>>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetExecutionChildrenEndpoint"/> class.
    /// </summary>
    protected GetExecutionChildrenEndpoint(IExecutionTracker tracker)
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
        Get("/executions/{Id}/children");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get execution children";
            s.Description = "Returns all direct child execution items for a parent execution.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecutionIdRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnFetchingChildren(req.Id);

        // First verify the execution exists
        var itemResult = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value == null)
        {
            OnExecutionNotFound(req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var childrenResult = await _tracker.GetChildren(req.Id, ct).ConfigureAwait(false);

        if (!childrenResult.IsSuccess)
        {
            var errorMessage = childrenResult.CurrentMessage ?? "Unknown error";
            OnChildrenFetchFailed(req.Id, errorMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch children", Details = errorMessage }, ct).ConfigureAwait(false);
            return;
        }

        var children = childrenResult.Value!;
        OnChildrenRetrieved(req.Id, children.Count);

        var childDtos = children
            .OrderBy(c => c.CreatedAt)
            .Select(MapToSummaryDto)
            .ToList();

        await Send.OkAsync(childDtos, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps an execution item to a summary DTO.
    /// </summary>
    protected virtual ExecutionSummaryResponse MapToSummaryDto(IExecutionItem item)
    {
        return new ExecutionSummaryResponse
        {
            Id = item.Id,
            ItemType = item.ItemType.Name,
            Name = item.Name,
            State = item.State.Name,
            CorrelationId = item.CorrelationId,
            CreatedAt = item.CreatedAt,
            CompletedAt = item.CompletedAt
        };
    }

    /// <summary>
    /// Called when fetching children. Override for custom logging.
    /// </summary>
    protected virtual void OnFetchingChildren(Guid id)
    {
    }

    /// <summary>
    /// Called when execution is not found. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionNotFound(Guid id)
    {
    }

    /// <summary>
    /// Called when children fetch fails. Override for custom logging.
    /// </summary>
    protected virtual void OnChildrenFetchFailed(Guid id, string error)
    {
    }

    /// <summary>
    /// Called when children are retrieved. Override for custom logging.
    /// </summary>
    protected virtual void OnChildrenRetrieved(Guid id, int count)
    {
    }
}
