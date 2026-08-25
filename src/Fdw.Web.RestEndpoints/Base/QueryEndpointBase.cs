using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Configuration;
using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Base;

/// <summary>
/// Base class for CQRS query operations with built-in pagination support.
/// Provides authentication, caching, and pagination handling.
/// </summary>
/// <typeparam name="TQuery">The query request type that extends PagedRequest.</typeparam>
/// <typeparam name="TResult">The result item type to be paginated.</typeparam>
public abstract class QueryEndpointBase<TQuery, TResult> : GenericEndpointBase<TQuery, PagedResponse<TResult>>
    where TQuery : PagedRequest, new()
{
    /// <summary>
    /// Configures the query endpoint with authentication, caching, and rate limiting.
    /// </summary>
    public override void Configure()
    {
        ConfigureAsGetEndpoint();
        Policies("RequireAuthentication");

        // Query caching with user-specific keys
        ResponseCache(EndpointDefaults.DefaultCacheDurationSeconds);
        Throttle(hitLimit: 100, durationSeconds: 60);

        ConfigureEndpoint();
    }

    /// <summary>
    /// Executes the query with pagination validation and processing.
    /// </summary>
    public override async Task<object> Execute(TQuery query, CancellationToken ct)
    {
        // Validate and normalize pagination parameters
        if (query.Page < 1) query.Page = 1;
        if (query.PageSize < 1 || query.PageSize > GetMaxPageSize())
            query.PageSize = GetDefaultPageSize();

        return await ExecuteQuery(query, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the actual query logic. Override this to implement your query.
    /// </summary>
    /// <param name="query">The validated query with normalized pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated response containing the query results.</returns>
    protected abstract Task<IGenericResult<PagedResponse<TResult>>> ExecuteQuery(TQuery query, CancellationToken ct);

    /// <summary>
    /// Gets the default page size when not specified in the request.
    /// </summary>
    protected virtual int GetDefaultPageSize() => 50;

    /// <summary>
    /// Gets the maximum allowed page size to prevent excessive data retrieval.
    /// </summary>
    protected virtual int GetMaxPageSize() => 1000;

    /// <summary>
    /// Configures this endpoint as a GET endpoint. Override to customize HTTP method or routing.
    /// </summary>
    protected virtual void ConfigureAsGetEndpoint()
    {
        // Override in derived classes to set route: Get("/api/endpoint");
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
        // Override in derived classes for additional configuration
    }
}

/// <summary>
/// Query endpoint for simple queries without custom request types.
/// Uses PagedRequest directly with query string parameters.
/// </summary>
/// <typeparam name="TResult">The result item type to be paginated.</typeparam>
public abstract class QueryEndpointBase<TResult> : QueryEndpointBase<PagedRequest, TResult>
{
}