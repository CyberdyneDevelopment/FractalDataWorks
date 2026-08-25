using System;
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
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}/{Name}".
    /// Override for ID-based or custom route patterns.
    /// </summary>
    protected virtual string Route => $"/{ResourceName}/{{Name}}";

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
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
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
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            if (ETagEnabled)
            {
                var etagProvider = TryResolve<IETagProvider>();
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
                            HttpContext.Response.StatusCode = 304;
                            return;
                        }

                        HttpContext.Response.Headers.ETag = etag;
                        HttpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    }
                }
            }

            var resourceName = GetResourceIdentifier(req);

            // Why this is a 400 and not the 404 the lookup would otherwise produce: an identifier
            // that names nothing means none arrived, and answering "not found" sends the next
            // person to the database instead of to the route that failed to bind.
            if (CrudResourceIdentifier.NamesNothing(resourceName))
            {
                HttpContext.Response.StatusCode = 400;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 400,
                    Title = "Missing identifier",
                    Detail = $"No {ResourceName} identifier was supplied.",
                    Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
                }, ct).ConfigureAwait(false);
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
                // Why: harness/API contract requires structured 404 body
                // {errorCode, messages[]} — Send.NotFoundAsync writes no body.
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(NotFoundProblem($"{ResourceName} '{resourceName}' was not found."), ct).ConfigureAwait(false);
                return;
            }

            OnAfterGet(resourceName);
            await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            HttpContext.Response.StatusCode = 500;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new FdwErrorResponse
            {
                Code = msg.Code ?? "INTERNAL_ERROR",
                Message = msg.Message,
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, ct).ConfigureAwait(false);
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

    /// <summary>
    /// Sends an error response when FindByIdentifier fails.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
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
        // Why fully qualified: FastEndpoints ships its own ProblemDetails and both are in scope
        // here. Both are RFC 7807 on the wire; this is the one ResultHttpStatusMapper emits, so
        // every failure from this endpoint has one shape whichever branch produced it.
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
