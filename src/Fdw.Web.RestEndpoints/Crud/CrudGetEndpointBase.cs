using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Web.RestEndpoints.Pagination;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Caching;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Abstract base class for get-by-name endpoints.
/// Provides routing, authorization, 404 handling, error handling,
/// and conditional GET via ETag/If-None-Match headers.
/// </summary>
/// <typeparam name="TRequest">The request type containing the resource identifier.</typeparam>
/// <typeparam name="TDetail">The detail DTO type returned for the resource.</typeparam>
public abstract class CrudGetEndpointBase<TRequest, TDetail> : Endpoint<TRequest, TDetail>
    where TRequest : notnull, new()
    where TDetail : class
{
    private readonly IETagProvider? _etagProvider;

    /// <summary>Initializes a new instance of the <see cref="CrudGetEndpointBase{TRequest, TDetail}"/> class.</summary>
    protected CrudGetEndpointBase(ILogger logger, IETagProvider? etagProvider = null)
    {
        Logger = logger;
        _etagProvider = etagProvider;
    }

    /// <summary>
    /// Gets the plural resource name used for routing and policy generation.
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
    /// Gets whether one record is read by <c>POST /{ResourceName}/get</c>, its identifier in the body, instead
    /// of by a GET that names it in the route.
    /// </summary>
    /// <remarks>
    /// A GET is a plain paged list and nothing else: a read of one record is a POST whose body names it
    /// (FDW-978). The request type is the same either way -- its identifier is bound from the body here and
    /// from the route there -- so an endpoint converts by overriding this alone. False keeps the GET route for
    /// the endpoints not yet converted.
    /// </remarks>
    protected virtual bool ReadByBody => false;

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}/{Name}", or "/{ResourceName}/get" when
    /// <see cref="ReadByBody"/>. Override for ID-based or custom route patterns.
    /// </summary>
    protected virtual string Route => ReadByBody ? $"/{ResourceName}/get" : $"/{ResourceName}/{{Name}}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"Get {ResourceName} by name";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Returns detailed information for a specific {ResourceName}.";

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
        if (ReadByBody)
            Post(Route);
        else
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
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
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

            var resourceName = GetResourceIdentifier(req);

            if (CrudResourceIdentifier.NamesNothing(resourceName))
            {
                await Send.ResultAsync(TypedResults.Json(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 400,
                    Title = "Missing identifier",
                    Detail = $"No {ResourceName} identifier was supplied.",
                    Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
                }, statusCode: 400)).ConfigureAwait(false);
                return;
            }
            OnBeforeGet(resourceName);

            var result = await FindByIdentifier(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await SendErrorResponse(result, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                OnNotFound(resourceName);
                await Send.ResultAsync(TypedResults.Json(NotFoundProblem($"{ResourceName} '{resourceName}' was not found."), statusCode: 404)).ConfigureAwait(false);
                return;
            }

            OnAfterGet(resourceName);
            await SendFound(result.Value!, ct).ConfigureAwait(false);
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
    /// Finds the resource by its identifier. Return a result with null value for 404.
    /// </summary>
    protected abstract Task<IGenericResult<TDetail?>> FindByIdentifier(TRequest request, CancellationToken ct);

    /// <summary>
    /// Extracts the human-readable resource identifier from the request (for logging).
    /// </summary>
    protected abstract string GetResourceIdentifier(TRequest request);

    /// <summary>
    /// Called before the get operation. Override for logging or pre-processing.
    /// </summary>
    protected virtual void OnBeforeGet(string identifier)
    {
    }

    /// <summary>
    /// Called when the resource is not found. Override for custom logging.
    /// </summary>
    protected virtual void OnNotFound(string identifier)
    {
    }

    /// <summary>
    /// Called after a successful get. Override for custom logging.
    /// </summary>
    protected virtual void OnAfterGet(string identifier)
    {
    }

    /// <summary>Sends the resource that was found.</summary>
    /// <param name="found">The resource, as <see cref="FindByIdentifier"/> returned it.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the send.</returns>
    /// <remarks>
    /// The resource as found, with status 200. An endpoint that answers some requests in another shape -- a page of the
    /// list it found, when the caller opts in -- overrides this alone, so a missing resource, a failed read and the
    /// conditional GET are answered exactly as they are for every other request (FDW-1194).
    /// </remarks>
    protected virtual Task SendFound(TDetail found, CancellationToken ct) => Send.OkAsync(found, ct);

    /// <summary>
    /// Sends the list the resource is as one page, when the caller asked for it with <c>?paged=true</c>.
    /// </summary>
    /// <typeparam name="TItem">The list's item type.</typeparam>
    /// <param name="items">The whole list the resource is, in the order a page follows.</param>
    /// <param name="selects">Says whether the search (<c>?q=</c>, <c>?match=</c>) selects an item.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True when the request was answered here, with a page or with the refusal of its paging or search; false when the caller did not ask for a page and the list is to be sent whole.</returns>
    /// <remarks>
    /// The page is cut in memory from the list the endpoint already read, in the envelope every list answers in, and a
    /// total (<c>?total=true</c>) counts the items the search selects. Without <c>paged</c> nothing here runs, so the
    /// endpoint answers exactly as it did before this existed (FDW-1194).
    /// </remarks>
    protected async Task<bool> SendPageWhenAsked<TItem>(IReadOnlyList<TItem> items, Func<TItem, ListSearch, bool> selects, CancellationToken ct)
    {
        var paged = QueryFlag.Read(HttpContext, "paged", Logger);
        if (paged.IsFailure)
        {
            await SendErrorResponse(paged, ct).ConfigureAwait(false);
            return true;
        }

        if (!paged.Value)
            return false;

        var paging = PaginationQuery.Parse(HttpContext, Logger);
        if (paging.IsFailure)
        {
            await SendErrorResponse(paging, ct).ConfigureAwait(false);
            return true;
        }

        var search = ListSearch.Parse(HttpContext, Logger);
        if (search.IsFailure)
        {
            await SendErrorResponse(search, ct).ConfigureAwait(false);
            return true;
        }

        var selected = items.Where(item => selects(item, search.Value!)).ToList();
        await Send.ResultAsync(TypedResults.Json(ListWindow.Of(selected, paging.Value!, search.Value!.IncludeTotal))).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Sends an error response when FindByIdentifier fails.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        return Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode));
    }

    /// <summary>Builds the RFC 7807 body for a resource that does not exist.</summary>
    /// <param name="detail">What was looked for.</param>
    /// <returns>The problem.</returns>
    /// <remarks>
    /// Why not an anonymous object: this branch answered 404 with {errorCode, messages[]} while every
    /// other failure from the same endpoint went through ResultHttpStatusMapper and came back as
    /// ProblemDetails. One endpoint, two error shapes, decided by which branch fired.
    /// </remarks>
    private Microsoft.AspNetCore.Mvc.ProblemDetails NotFoundProblem(string detail)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = 404,
            Title = "Not found",
            Detail = detail,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        };

        problem.Extensions["code"] = "NotFound";
        problem.Extensions["referenceId"] = HttpContext.TraceIdentifier;
        problem.Extensions["isRetryable"] = false;
        return problem;
    }
}
