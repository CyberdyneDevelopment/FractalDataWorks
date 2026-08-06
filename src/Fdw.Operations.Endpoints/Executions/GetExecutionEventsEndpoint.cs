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
/// Endpoint to get events for an execution.
/// </summary>
public abstract class GetExecutionEventsEndpoint : Endpoint<ExecutionIdRequest, List<ExecutionEventDto>>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetExecutionEventsEndpoint"/> class.
    /// </summary>
    protected GetExecutionEventsEndpoint(IExecutionTracker tracker)
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
        Get("/executions/{Id}/events");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get execution events";
            s.Description = "Returns the event history for a specific execution, ordered by sequence number.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecutionIdRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnFetchingEvents(req.Id);

        // First verify the execution exists
        var itemResult = await _tracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value == null)
        {
            OnExecutionNotFound(req.Id);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var eventsResult = await _tracker.GetEvents(req.Id, ct).ConfigureAwait(false);

        if (!eventsResult.IsSuccess)
        {
            var errorMessage = eventsResult.CurrentMessage ?? "Unknown error";
            OnEventsFetchFailed(req.Id, errorMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch events", Details = errorMessage }, ct).ConfigureAwait(false);
            return;
        }

        var events = eventsResult.Value!;
        OnEventsRetrieved(req.Id, events.Count);

        var eventDtos = events
            .OrderBy(e => e.SequenceNumber)
            .Select(MapToEventDto)
            .ToList();

        await Send.OkAsync(eventDtos, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps an execution event to a DTO.
    /// </summary>
    protected virtual ExecutionEventDto MapToEventDto(IExecutionEvent e)
    {
        return new ExecutionEventDto
        {
            Id = e.Id,
            ExecutionItemId = e.ExecutionItemId,
            SequenceNumber = e.SequenceNumber,
            EventType = e.EventType,
            PreviousState = e.PreviousState,
            NewState = e.NewState,
            Message = e.Message,
            Data = e.Data,
            Actor = e.Actor,
            Timestamp = e.Timestamp
        };
    }

    /// <summary>
    /// Called when fetching events. Override for custom logging.
    /// </summary>
    protected virtual void OnFetchingEvents(Guid id)
    {
    }

    /// <summary>
    /// Called when execution is not found. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionNotFound(Guid id)
    {
    }

    /// <summary>
    /// Called when events fetch fails. Override for custom logging.
    /// </summary>
    protected virtual void OnEventsFetchFailed(Guid id, string error)
    {
    }

    /// <summary>
    /// Called when events are retrieved. Override for custom logging.
    /// </summary>
    protected virtual void OnEventsRetrieved(Guid id, int count)
    {
    }
}
