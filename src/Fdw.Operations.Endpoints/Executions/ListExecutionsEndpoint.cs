using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Web.Endpoints;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to list executions with pagination and filters.
/// </summary>
public abstract class ListExecutionsEndpoint : Endpoint<ListExecutionsRequest, PaginatedResponse<ExecutionSummaryResponse>>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListExecutionsEndpoint"/> class.
    /// </summary>
    protected ListExecutionsEndpoint(IExecutionTracker tracker)
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
        Get("/executions");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "List executions";
            s.Description = "Returns a paginated list of executions with optional filtering by correlation ID, item type, state, or root ID.";
            s.ExampleRequest = new ListExecutionsRequest { Page = 1, PageSize = 25 };
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListExecutionsRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnListingExecutions(req.ValidatedPage, req.ValidatedPageSize);

        // If correlation ID is provided, use the specialized query
        if (!string.IsNullOrWhiteSpace(req.CorrelationId))
        {
            OnQueryingByCorrelationId(req.CorrelationId);
            var correlationResult = await _tracker.GetItems(req.CorrelationId, ct).ConfigureAwait(false);

            if (!correlationResult.IsSuccess)
            {
                var errorMessage = correlationResult.CurrentMessage ?? "Unknown error";
                OnExecutionQueryFailed(errorMessage);
                HttpContext.Response.StatusCode = 500;
                await HttpContext.Response.WriteAsJsonAsync(
                    new { Error = "Failed to query executions", Details = errorMessage }, ct).ConfigureAwait(false);
                return;
            }

            var correlationItems = correlationResult.Value!;
            var filteredItems = ApplyFilters(correlationItems, req);
            var pagedItems = filteredItems
                .OrderByDescending(x => x.CreatedAt)
                .Skip(req.Skip)
                .Take(req.ValidatedPageSize)
                .Select(MapToSummaryDto)
                .ToList();

            OnExecutionsListed(filteredItems.Count);

            await Send.OkAsync(new PaginatedResponse<ExecutionSummaryResponse>
            {
                Items = pagedItems,
                Page = req.ValidatedPage,
                PageSize = req.ValidatedPageSize,
                TotalCount = filteredItems.Count
            }, ct).ConfigureAwait(false);
            return;
        }

        // Why: Resolve item type and state from string names to TypeCollection entries.
        // ByName returns NotFound sentinel when name doesn't match — treat as no filter.
        IExecutionItemType? itemTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(req.ItemType))
        {
            var resolved = ExecutionItemTypes.ByName(req.ItemType);
            if (resolved != ExecutionItemTypes.NotFound)
                itemTypeFilter = resolved;
        }

        IExecutionStateType? stateFilter = null;
        if (!string.IsNullOrWhiteSpace(req.State))
        {
            var resolved = ExecutionStateTypes.ByName(req.State);
            if (resolved != ExecutionStateTypes.NotFound)
                stateFilter = resolved;
        }

        var listResult = await _tracker.ListExecutions(
            req.ValidatedPage, req.ValidatedPageSize,
            itemTypeFilter, stateFilter, req.CorrelationId, ct).ConfigureAwait(false);

        if (!listResult.IsSuccess || listResult.Value == null)
        {
            var errorMessage = listResult.CurrentMessage ?? "Unknown error";
            OnExecutionQueryFailed(errorMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to query executions", Details = errorMessage }, ct).ConfigureAwait(false);
            return;
        }

        var pagedResult = listResult.Value;
        OnExecutionsListed(pagedResult.Items.Count);

        await Send.OkAsync(new PaginatedResponse<ExecutionSummaryResponse>
        {
            Items = pagedResult.Items.Select(MapToSummaryDto).ToList(),
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalCount = (int)pagedResult.TotalCount
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies filters to execution items.
    /// </summary>
    protected virtual IList<IExecutionItem> ApplyFilters(IReadOnlyList<IExecutionItem> items, ListExecutionsRequest req)
    {
        var filtered = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(req.ItemType))
        {
            filtered = filtered.Where(x => string.Equals(x.ItemType.Name, req.ItemType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(req.State))
        {
            filtered = filtered.Where(x => string.Equals(x.State.Name, req.State, StringComparison.OrdinalIgnoreCase));
        }

        if (req.RootId.HasValue)
        {
            filtered = filtered.Where(x => x.RootId == req.RootId.Value);
        }

        return filtered.ToList();
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
    /// Called when listing executions. Override for custom logging.
    /// </summary>
    protected virtual void OnListingExecutions(int page, int pageSize)
    {
    }

    /// <summary>
    /// Called when querying by correlation ID. Override for custom logging.
    /// </summary>
    protected virtual void OnQueryingByCorrelationId(string correlationId)
    {
    }

    /// <summary>
    /// Called when execution query fails. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionQueryFailed(string error)
    {
    }

    /// <summary>
    /// Called when executions are listed. Override for custom logging.
    /// </summary>
    protected virtual void OnExecutionsListed(int count)
    {
    }
}
