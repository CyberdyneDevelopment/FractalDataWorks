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
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Endpoint to list executions with pagination and filters.
/// </summary>
public abstract class ListExecutionsEndpointBase : Endpoint<ListExecutionsRequest, PaginatedResponse<ExecutionSummaryResponse>>
{
    private readonly IExecutionTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListExecutionsEndpointBase"/> class.
    /// </summary>
    protected ListExecutionsEndpointBase(ILogger logger, IExecutionTracker tracker)
    {
        EndpointLogger = logger;
        _tracker = tracker;
    }

    /// <summary>
    /// Gets the execution tracker.
    /// </summary>
    protected IExecutionTracker Tracker => _tracker;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

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
            s.Description = "Returns a paginated list of executions with optional filtering by correlation ID, item type, state, or root ID. With no item type, root ID or correlation ID the list holds runs (Job items) only; ask for a root or a correlation ID to get every item of that family, or name an item type.";
            s.ExampleRequest = new ListExecutionsRequest { Page = 1, PageSize = 25 };
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListExecutionsRequest req, CancellationToken ct)
    {
        var paging = PaginationQuery.ResolvePage(HttpContext, EndpointLogger, req.Page, req.PageSize);
        if (paging.IsFailure)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(paging, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var page = paging.Value!;
        OnListingExecutions(req.Page, page.Take);
        var itemTypeFilter = ExecutionListItemType.Of(req.ItemType, req.RootId.HasValue, !string.IsNullOrWhiteSpace(req.CorrelationId), out var refusal);
        if (refusal is not null)
        {
            ThrowError(refusal, 400);
        }

        // If correlation ID is provided, use the specialized query
        if (!string.IsNullOrWhiteSpace(req.CorrelationId))
        {
            OnQueryingByCorrelationId(req.CorrelationId);
            var correlationResult = await _tracker.GetItems(req.CorrelationId, ct).ConfigureAwait(false);

            if (!correlationResult.IsSuccess)
            {
                var errorMessage = correlationResult.CurrentMessage ?? "Unknown error";
                OnExecutionQueryFailed(errorMessage);
                await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to query executions", Details = errorMessage }, statusCode: 500)).ConfigureAwait(false);
                return;
            }

            var correlationItems = correlationResult.Value!;
            var filteredItems = ApplyFilters(correlationItems, req);
            var pagedItems = filteredItems
                .OrderByDescending(x => x.CreatedAt)
                .Skip(page.Skip)
                .Take(page.Take)
                .Select(ExecutionSummaryResponse.Of)
                .ToList();

            OnExecutionsListed(filteredItems.Count);

            await Send.OkAsync(new PaginatedResponse<ExecutionSummaryResponse>
            {
                Items = pagedItems,
                Page = req.Page,
                PageSize = page.Take,
                TotalCount = filteredItems.Count
            }, ct).ConfigureAwait(false);
            return;
        }

        IExecutionStateType? stateFilter = null;
        if (!string.IsNullOrWhiteSpace(req.State))
        {
            var resolved = ExecutionStateTypes.ByName(req.State);
            if (resolved != ExecutionStateTypes.NotFound)
                stateFilter = resolved;
        }

        var listResult = await _tracker.ListExecutions(
            req.Page, page.Take,
            itemTypeFilter, stateFilter, req.CorrelationId, req.RootId, ct).ConfigureAwait(false);

        if (!listResult.IsSuccess || listResult.Value == null)
        {
            var errorMessage = listResult.CurrentMessage ?? "Unknown error";
            OnExecutionQueryFailed(errorMessage);
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to query executions", Details = errorMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var pagedResult = listResult.Value;
        OnExecutionsListed(pagedResult.Items.Count);

        await Send.OkAsync(new PaginatedResponse<ExecutionSummaryResponse>
        {
            Items = pagedResult.Items.Select(ExecutionSummaryResponse.Of).ToList(),
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
