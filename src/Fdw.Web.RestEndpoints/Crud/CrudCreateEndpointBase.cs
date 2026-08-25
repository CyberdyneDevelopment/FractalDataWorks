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
/// Abstract base class for resource creation endpoints.
/// Provides uniqueness checking (409), validation, creation (201), and error handling.
/// </summary>
/// <typeparam name="TCreateRequest">The create request type with the new resource's properties.</typeparam>
/// <typeparam name="TDetail">The detail DTO type returned after creation.</typeparam>
public abstract class CrudCreateEndpointBase<TCreateRequest, TDetail> : Endpoint<TCreateRequest, TDetail>
    where TCreateRequest : notnull, new()
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
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}".
    /// </summary>
    protected virtual string Route => $"/{ResourceName}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"Create a new {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Creates a new {ResourceName} configuration.";

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Post(Route);
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
    public override async Task HandleAsync(TCreateRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            var resourceName = GetResourceName(req);
            OnBeforeCreate(resourceName);

            // Check for duplicates
            var existsResult = await CheckExists(req, ct).ConfigureAwait(false);
            if (!existsResult.IsSuccess)
            {
                await SendErrorResponse(existsResult, "check existence", ct).ConfigureAwait(false);
                return;
            }

            if (existsResult.Value)
            {
                OnAlreadyExists(resourceName);
                ThrowError($"A {ResourceName} with this name already exists", 409);
                return;
            }

            // Run custom validation
            var validationResult = await ValidateCreate(req, ct).ConfigureAwait(false);
            if (!validationResult.IsSuccess)
            {
                await SendErrorResponse(validationResult, "validate", ct).ConfigureAwait(false);
                return;
            }

            // Perform creation
            var createResult = await Create(req, ct).ConfigureAwait(false);
            if (!createResult.IsSuccess)
            {
                await SendErrorResponse(createResult, "create", ct).ConfigureAwait(false);
                return;
            }

            OnAfterCreate(resourceName);
            await SendCreatedResponse(createResult.Value!, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not ValidationFailureException)
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
    /// Extracts the resource name from the create request (for logging and duplicate checking).
    /// </summary>
    protected abstract string GetResourceName(TCreateRequest request);

    /// <summary>
    /// Checks whether a resource with the same name already exists. Return true for conflict.
    /// </summary>
    protected abstract Task<IGenericResult<bool>> CheckExists(TCreateRequest request, CancellationToken ct);

    /// <summary>
    /// Performs the actual resource creation. Returns the created detail DTO.
    /// </summary>
    protected abstract Task<IGenericResult<TDetail>> Create(TCreateRequest request, CancellationToken ct);

    /// <summary>
    /// Validates the create request beyond attribute-level validation.
    /// Override for custom business rules. Default returns success.
    /// </summary>
    protected virtual Task<IGenericResult> ValidateCreate(TCreateRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <summary>
    /// Sends the 201 Created response. Override to customize (e.g., add Location header via CreatedAtAsync).
    /// </summary>
    protected virtual Task SendCreatedResponse(TDetail detail, CancellationToken ct)
        => Send.ResponseAsync(detail, 201, ct);

    /// <summary>
    /// Called before creation begins. Override for logging.
    /// </summary>
    protected virtual void OnBeforeCreate(string resourceName)
    {
    }

    /// <summary>
    /// Called when a duplicate is detected. Override for custom logging.
    /// </summary>
    protected virtual void OnAlreadyExists(string resourceName)
    {
    }

    /// <summary>
    /// Called after successful creation. Override for logging.
    /// </summary>
    protected virtual void OnAfterCreate(string resourceName)
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
}
