using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Caching;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Abstract base class for list/summary endpoints that return a collection of resources.
/// Provides configurable routing, policy-based authorization, error handling,
/// and pagination via <c>?skip=</c> and <c>?take=</c>, sized by the <c>DefaultPaginationSize</c> and
/// <c>MaxPaginationSize</c> settings.
/// </summary>
/// <typeparam name="TSummary">The summary DTO type returned in the list.</typeparam>
public abstract class CrudListEndpointBase<TSummary> : EndpointWithoutRequest<List<TSummary>>
    where TSummary : class
{
    private readonly IETagProvider? _etagProvider;

    /// <summary>Initializes a new instance of the <see cref="CrudListEndpointBase{TSummary}"/> class.</summary>
    protected CrudListEndpointBase(
        ILogger logger,
        IETagProvider? etagProvider = null)
    {
        Logger = logger;
        _etagProvider = etagProvider;
    }

    /// <summary>
    /// Gets the plural resource name used for routing and policy generation (e.g., "connections", "datastores").
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>Gets the documentation tag this endpoint appears under.</summary>
    /// <remarks>
    /// Derived from the resource, like the route and the policy beside it, rather than stated by
    /// each endpoint. Stated, it was the same string repeated for every endpoint over a resource -
    /// and a wrong route 404s where a wrong tag just drops the endpoint out of its group in the
    /// documentation, with nothing to say so.
    /// </remarks>
    protected virtual string EndpointTag => ResourceName;

    /// <summary>
    /// Gets the read policy for this endpoint. Defaults to "{ResourceName}:read".
    /// Override to customize authorization.
    /// </summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets whether the resource this endpoint acts on decides who may use it, instead of a platform permission.
    /// </summary>
    /// <remarks>
    /// When true the endpoint requires only a signed-in caller, and the endpoint itself asks the
    /// resource's own access policy. A workspace is the case this exists for: ownership, an
    /// in-effect membership or a platform permission can each grant access to one, so the platform
    /// permission cannot be the gate in front of it (FDW-936). False keeps the platform permission
    /// as the gate, which is what every other resource does.
    /// </remarks>
    protected virtual bool ResourceDecidesAccess => false;

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}".
    /// Override to customize the route pattern.
    /// </summary>
    protected virtual string Route => $"/{ResourceName}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"List {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Returns a paginated list of {ResourceName}.";

    /// <summary>
    /// Gets whether ETag-based conditional GET is enabled for this endpoint.
    /// When true and an <see cref="IETagProvider"/> is registered, the endpoint checks
    /// the If-None-Match header and returns 304 Not Modified when the ETag matches.
    /// Default is true. Override to disable.
    /// </summary>
    protected virtual bool ETagEnabled => true;

    /// <summary>
    /// Gets the container name used for ETag generation.
    /// Defaults to <see cref="ResourceName"/>. Override if the data container differs from the route resource.
    /// </summary>
    protected virtual string ETagContainerName => ResourceName;

    /// <summary>
    /// Gets the connection name used for ETag generation.
    /// Defaults to "Default". Override to target a specific connection.
    /// </summary>
    protected virtual string ETagConnectionName => "Default";

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        if (!ResourceDecidesAccess)
            Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

        Description(x => x.WithTags(EndpointTag));

    ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup
    /// such as caching, throttling, or tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            if (ETagEnabled)
            {
                var etagProvider = _etagProvider;
                if (etagProvider is not null)
                {
                    var etag = await etagProvider.GetETag(ETagContainerName, ETagConnectionName, ct)
                        .ConfigureAwait(false);

                    if (etag is not null)
                    {
                        var ifNoneMatch = HttpContext.Request.Headers.IfNoneMatch.ToString();
                        if (string.Equals(etag, ifNoneMatch, StringComparison.Ordinal))
                        {
                            ETagLogger.ETagComputed(Logger, ETagContainerName, etag);
                            await Send.NotModifiedAsync(ct).ConfigureAwait(false);
                            return;
                        }

                        HttpContext.Response.Headers.ETag = etag;
                        HttpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    }
                }
            }

            var paging = PaginationQuery.Parse(HttpContext, Logger);
            if (paging.IsFailure)
            {
                await SendErrorResponse(paging, ct).ConfigureAwait(false);
                return;
            }

            EndpointLogger.PaginatedListRequest(Logger, GetType().Name, paging.Value!.Skip, paging.Value.Take);

            var result = await LoadItems(paging.Value, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await SendErrorResponse(result, ct).ConfigureAwait(false);
                return;
            }

            var response = paging.Value.Apply(result.Value!);

            EndpointLogger.PaginatedListResponse(Logger, GetType().Name, response.Items.Count, response.TotalCount);

            await SendPaginatedResponse(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            await Send.ResultAsync(TypedResults.Json(new FdwErrorResponse
            {
                Code = msg.Code ?? "INTERNAL_ERROR",
                Message = msg.Message,
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, statusCode: 500)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads the list of resource summaries with pagination context.
    /// The default implementation delegates to <see cref="LoadItems(CancellationToken)"/>
    /// for backward compatibility. Override to implement server-side pagination
    /// using the paging parameters directly.
    /// </summary>
    /// <param name="paging">The pagination parameters from the query string.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the list of summaries.</returns>
    protected virtual Task<IGenericResult<List<TSummary>>> LoadItems(PaginatedListRequest paging, CancellationToken ct)
    {
        return LoadItems(ct);
    }

    /// <summary>
    /// Loads the list of resource summaries. Override to implement data retrieval.
    /// </summary>
    /// <remarks>
    /// When only this method is overridden, pagination is applied in-memory by the base class.
    /// For server-side pagination, override <see cref="LoadItems(PaginatedListRequest, CancellationToken)"/> instead.
    /// </remarks>
    protected abstract Task<IGenericResult<List<TSummary>>> LoadItems(CancellationToken ct);

    /// <summary>
    /// Sends the paginated response. Override to customize the response format.
    /// </summary>
    /// <param name="response">The paginated response to send.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    protected virtual Task SendPaginatedResponse(PaginatedResponse<TSummary> response, CancellationToken ct)
        => Send.ResultAsync(TypedResults.Json(response));

    /// <summary>
    /// Sends an error response when LoadItems fails. Override to customize error format.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        return Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode));
    }
}

/// <summary>
/// Abstract base class for paginated list endpoints.
/// Extends CrudListEndpointBase with request-based filtering, pagination,
/// and conditional GET via ETag/If-None-Match headers.
/// </summary>
/// <typeparam name="TListRequest">The list request type with pagination/filter parameters.</typeparam>
/// <typeparam name="TSummary">The summary DTO type returned in the list.</typeparam>
public abstract class CrudListEndpointBase<TListRequest, TSummary> : Endpoint<TListRequest, List<TSummary>>
    where TListRequest : notnull, new()
    where TSummary : class
{
    private readonly IETagProvider? _etagProvider;

    /// <summary>Initializes a new instance of the <see cref="CrudListEndpointBase{TListRequest, TSummary}"/> class.</summary>
    protected CrudListEndpointBase(
        ILogger logger,
        IETagProvider? etagProvider = null)
    {
        Logger = logger;
        _etagProvider = etagProvider;
    }

    /// <summary>
    /// Gets the plural resource name used for routing and policy generation.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>Gets the documentation tag this endpoint appears under.</summary>
    /// <remarks>Derived from the resource, like the route and policy beside it.</remarks>
    protected virtual string EndpointTag => ResourceName;

    /// <summary>
    /// Gets the read policy for this endpoint.
    /// </summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets whether the resource this endpoint acts on decides who may use it, instead of a platform permission.
    /// </summary>
    /// <remarks>
    /// When true the endpoint requires only a signed-in caller, and the endpoint itself asks the
    /// resource's own access policy. A workspace is the case this exists for: ownership, an
    /// in-effect membership or a platform permission can each grant access to one, so the platform
    /// permission cannot be the gate in front of it (FDW-936). False keeps the platform permission
    /// as the gate, which is what every other resource does.
    /// </remarks>
    protected virtual bool ResourceDecidesAccess => false;

    /// <summary>
    /// Gets the route for this endpoint.
    /// </summary>
    protected virtual string Route => $"/{ResourceName}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"List {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Returns a paginated list of {ResourceName} with optional filtering.";

    /// <summary>
    /// Gets whether ETag-based conditional GET is enabled for this endpoint.
    /// When true and an <see cref="IETagProvider"/> is registered, the endpoint checks
    /// the If-None-Match header and returns 304 Not Modified when the ETag matches.
    /// Default is true. Override to disable.
    /// </summary>
    protected virtual bool ETagEnabled => true;

    /// <summary>
    /// Gets the container name used for ETag generation.
    /// Defaults to <see cref="ResourceName"/>. Override if the data container differs from the route resource.
    /// </summary>
    protected virtual string ETagContainerName => ResourceName;

    /// <summary>
    /// Gets the connection name used for ETag generation.
    /// Defaults to "Default". Override to target a specific connection.
    /// </summary>
    protected virtual string ETagConnectionName => "Default";

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        if (!ResourceDecidesAccess)
            Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

        Description(x => x.WithTags(EndpointTag));

    ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(TListRequest req, CancellationToken ct)
    {
        try
        {
            if (ETagEnabled)
            {
                var etagProvider = _etagProvider;
                if (etagProvider is not null)
                {
                    var etag = await etagProvider.GetETag(ETagContainerName, ETagConnectionName, ct)
                        .ConfigureAwait(false);

                    if (etag is not null)
                    {
                        var ifNoneMatch = HttpContext.Request.Headers.IfNoneMatch.ToString();
                        if (string.Equals(etag, ifNoneMatch, StringComparison.Ordinal))
                        {
                            ETagLogger.ETagComputed(Logger, ETagContainerName, etag);
                            await Send.NotModifiedAsync(ct).ConfigureAwait(false);
                            return;
                        }

                        HttpContext.Response.Headers.ETag = etag;
                        HttpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    }
                }
            }

            var result = await LoadItems(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await SendErrorResponse(result, ct).ConfigureAwait(false);
                return;
            }

            // Why the page is cut here rather than left to LoadItems: this envelope used to go out
            // with skip 0 and take = count, claiming a page while carrying the whole table.
            var paging = ResolvePaging(req);
            if (paging.IsFailure)
            {
                await SendErrorResponse(paging, ct).ConfigureAwait(false);
                return;
            }

            EndpointLogger.PaginatedListRequest(Logger, GetType().Name, paging.Value!.Skip, paging.Value.Take);

            var response = paging.Value.Apply(result.Value!);

            EndpointLogger.PaginatedListResponse(Logger, GetType().Name, response.Items.Count, response.TotalCount);
            await Send.ResultAsync(TypedResults.Json(response)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            await Send.ResultAsync(TypedResults.Json(new FdwErrorResponse
            {
                Code = msg.Code ?? "INTERNAL_ERROR",
                Message = msg.Message,
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, statusCode: 500)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads the list of resource summaries based on the request parameters.
    /// </summary>
    protected abstract Task<IGenericResult<List<TSummary>>> LoadItems(TListRequest request, CancellationToken ct);

    /// <summary>
    /// Resolves which page of the loaded list to send.
    /// </summary>
    /// <param name="request">The bound request.</param>
    /// <returns>The page to cut, by default read from <c>?skip=</c> and <c>?take=</c>; a failure when the request's paging is invalid or a paging setting is missing.</returns>
    /// <remarks>
    /// Override when the request type declares its own paging fields (for example Page/PageSize),
    /// so the fields a caller can see in the contract are the ones that take effect. Size the page
    /// with <see cref="PaginationQuery.Resolve"/> so it honours the same settings as every other list.
    /// </remarks>
    protected virtual IGenericResult<PaginatedListRequest> ResolvePaging(TListRequest request)
        => PaginationQuery.Parse(HttpContext, Logger);

    /// <summary>
    /// Sends an error response when LoadItems fails.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        return Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode));
    }
}
