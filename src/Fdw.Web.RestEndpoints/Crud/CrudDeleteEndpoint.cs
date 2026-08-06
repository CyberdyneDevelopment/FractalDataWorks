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
/// Abstract base class for resource deletion endpoints.
/// Provides existence checking (404), pre-delete validation, deletion (204), and error handling.
/// </summary>
/// <typeparam name="TRequest">The request type containing the resource identifier.</typeparam>
public abstract class CrudDeleteEndpoint<TRequest> : Endpoint<TRequest, object>
    where TRequest : notnull, new()
{
    /// <summary>
    /// Gets the plural resource name used for routing and policy generation.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Gets the delete policy for this endpoint.
    /// </summary>
    protected virtual string DeletePolicy => $"{ResourceName}:delete";

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}/{Name}".
    /// </summary>
    protected virtual string Route => $"/{ResourceName}/{{Name}}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"Delete a {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Deletes a {ResourceName} configuration.";

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(DeletePolicy);
#endif
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

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
            var identifier = GetResourceIdentifier(req);
            OnBeforeDelete(identifier);

            // Check existence
            var existsResult = await CheckExistsForDelete(req, ct).ConfigureAwait(false);
            if (!existsResult.IsSuccess)
            {
                await SendErrorResponse(existsResult, "find", ct).ConfigureAwait(false);
                return;
            }

            if (!existsResult.Value)
            {
                OnNotFound(identifier);
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = "NotFound",
                    messages = new[] { $"{ResourceName} '{identifier}' was not found." }
                }, ct).ConfigureAwait(false);
                return;
            }

            // Pre-delete validation (e.g., check for dependent resources)
            var validationResult = await ValidateDelete(req, ct).ConfigureAwait(false);
            if (!validationResult.IsSuccess)
            {
                await SendErrorResponse(validationResult, "validate", ct).ConfigureAwait(false);
                return;
            }

            // Perform deletion
            var deleteResult = await Delete(req, ct).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
            {
                await SendErrorResponse(deleteResult, "delete", ct).ConfigureAwait(false);
                return;
            }

            OnAfterDelete(identifier);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
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
    protected abstract string GetResourceIdentifier(TRequest request);

    /// <summary>
    /// Checks whether the resource exists. Return true if it exists (can be deleted).
    /// </summary>
    protected abstract Task<IGenericResult<bool>> CheckExistsForDelete(TRequest request, CancellationToken ct);

    /// <summary>
    /// Performs the actual deletion.
    /// </summary>
    protected abstract Task<IGenericResult> Delete(TRequest request, CancellationToken ct);

    /// <summary>
    /// Validates that the resource can be safely deleted (e.g., no dependent resources).
    /// Override to add referential integrity checks. Default returns success.
    /// </summary>
    protected virtual Task<IGenericResult> ValidateDelete(TRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <summary>
    /// Called before the delete operation. Override for logging.
    /// </summary>
    protected virtual void OnBeforeDelete(string identifier)
    {
    }

    /// <summary>
    /// Called when the resource to delete is not found. Override for logging.
    /// </summary>
    protected virtual void OnNotFound(string identifier)
    {
    }

    /// <summary>
    /// Called after successful deletion. Override for logging.
    /// </summary>
    protected virtual void OnAfterDelete(string identifier)
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
