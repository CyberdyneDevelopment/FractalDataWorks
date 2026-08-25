using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Abstract base class for resource update endpoints.
/// Provides find-existing (404), validation, partial update, and error handling.
/// </summary>
/// <typeparam name="TUpdateRequest">The update request type with nullable properties for partial updates.</typeparam>
/// <typeparam name="TDetail">The detail DTO type returned after update.</typeparam>
public abstract class CrudUpdateEndpointBase<TUpdateRequest, TDetail> : Endpoint<TUpdateRequest, TDetail>
    where TUpdateRequest : notnull, new()
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
    /// Gets the write policy for this endpoint.
    /// </summary>
    protected virtual string WritePolicy => $"{ResourceName}:write";

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}/{Name}".
    /// </summary>
    protected virtual string Route => $"/{ResourceName}/{{Name}}";

    /// <summary>
    /// Gets the HTTP method for updates.
    /// </summary>
    /// <remarks>
    /// PATCH, because an update here names what is changing rather than replacing a resource
    /// whole — this surface has no PUT. An endpoint that performs a named action rather than
    /// changing fields is a POST and does not derive from this base at all.
    /// </remarks>
    protected virtual HttpVerb UpdateVerb => HttpVerb.PATCH;

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"Update a {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Updates an existing {ResourceName} configuration.";

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Verbs(UpdateVerb.ToString().ToUpperInvariant());
        Routes(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
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
    public override async Task HandleAsync(TUpdateRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
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
            OnBeforeUpdate(resourceName);

            // Find existing resource
            var findResult = await FindForUpdate(req, ct).ConfigureAwait(false);
            if (!findResult.IsSuccess)
            {
                await SendErrorResponse(findResult, "find", ct).ConfigureAwait(false);
                return;
            }

            if (findResult.Value is null)
            {
                OnNotFound(resourceName);
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(NotFoundProblem($"{ResourceName} '{resourceName}' was not found."), ct).ConfigureAwait(false);
                return;
            }

            // Validate update
            var validationResult = await ValidateUpdate(req, findResult.Value, ct).ConfigureAwait(false);
            if (!validationResult.IsSuccess)
            {
                await SendErrorResponse(validationResult, "validate", ct).ConfigureAwait(false);
                return;
            }

            // Perform update
            var updateResult = await Update(req, findResult.Value, ct).ConfigureAwait(false);
            if (!updateResult.IsSuccess)
            {
                await SendErrorResponse(updateResult, "update", ct).ConfigureAwait(false);
                return;
            }

            OnAfterUpdate(resourceName);
            await Send.OkAsync(updateResult.Value!, ct).ConfigureAwait(false);
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
    /// Extracts the human-readable resource identifier from the request.
    /// </summary>
    protected abstract string GetResourceIdentifier(TUpdateRequest request);

    /// <summary>
    /// Finds the existing resource for update. Return null value for 404.
    /// The returned object represents the current state before update.
    /// </summary>
    protected abstract Task<IGenericResult<TDetail?>> FindForUpdate(TUpdateRequest request, CancellationToken ct);

    /// <summary>
    /// Performs the update by merging the request into the existing resource.
    /// Returns the updated detail DTO.
    /// </summary>
    protected abstract Task<IGenericResult<TDetail>> Update(TUpdateRequest request, TDetail existing, CancellationToken ct);

    /// <summary>
    /// Validates the update request against the existing resource.
    /// Override for custom business rules. Default returns success.
    /// </summary>
    protected virtual Task<IGenericResult> ValidateUpdate(TUpdateRequest request, TDetail existing, CancellationToken ct)
        => Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <summary>
    /// Called before the update operation. Override for logging.
    /// </summary>
    protected virtual void OnBeforeUpdate(string identifier)
    {
    }

    /// <summary>
    /// Called when the resource to update is not found. Override for logging.
    /// </summary>
    protected virtual void OnNotFound(string identifier)
    {
    }

    /// <summary>
    /// Called after successful update. Override for logging.
    /// </summary>
    protected virtual void OnAfterUpdate(string identifier)
    {
    }

    /// <summary>
    /// Sends an error response for failed operations.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, string operation, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }

    /// <summary>
    /// HTTP verbs for update operations.
    /// </summary>
#pragma warning disable FDW017
    protected enum HttpVerb
    {
        /// <summary>Full replacement update.</summary>
        PUT,
        /// <summary>Partial update.</summary>
        PATCH
    }
#pragma warning restore FDW017

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
