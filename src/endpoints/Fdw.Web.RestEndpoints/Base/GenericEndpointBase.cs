using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Orchestration.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Web.RestEndpoints.Base;

/// <summary>
/// Base endpoint class that provides authentication, RBAC, and Fdw service integration.
/// Inherits from FastEndpoints for natural HTTP handling. It holds no gateway, executor or domain service: an endpoint
/// built on it takes its own domain's provider in its own constructor (FDW-4618 rule 1, FDW-4628).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class GenericEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenericEndpointBase{TRequest, TResponse}"/> class.
    /// </summary>
    protected GenericEndpointBase(
        ILogger<GenericEndpointBase<TRequest, TResponse>> logger)
    {
        Logger = logger ?? NullLogger<GenericEndpointBase<TRequest, TResponse>>.Instance;
    }

    /// <summary>
    /// Gets the logger instance for this endpoint.
    /// </summary>
    protected new ILogger Logger { get; }

    /// <summary>
    /// Handles the HTTP request with authentication, authorization, and Fdw result patterns.
    /// </summary>
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        try
        {
            // Check authorization if required
            var authResult = await CheckAuthorization(req, ct).ConfigureAwait(false);
            if (!authResult.IsSuccess)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(WebRestEndpointsAnswerLog.GenericNotSignedIn(Logger)), HttpContext)).ConfigureAwait(false);
                return;
            }

            // Execute business logic
            var result = await Execute(req, ct).ConfigureAwait(false);

            // Handle IGenericResult conversion to FastEndpoints responses
            if (result is IGenericResult<TResponse> recResult)
            {
                if (recResult.IsSuccess && recResult.Value != null)
                    Response = recResult.Value; // FastEndpoints sends 200 OK
                else
                    await Send.StatusCodeAsync(400, ct).ConfigureAwait(false);
            }
            else if (result is IOrchestrationResult orchestrationResult)
            {
                if (orchestrationResult.Status.IsSuccess)
                {
                    if (orchestrationResult.Output is TResponse typedOutput)
                        Response = typedOutput;
                    else
                        HttpContext.Response.StatusCode = 200;
                }
                else
                {
                    var orchMsg = EndpointLogger.EndpointError(Logger, new InvalidOperationException(orchestrationResult.Status.ToString()), GetType().Name);
                    await Send.ResultAsync(TypedResults.Json(new FdwErrorResponse
                    {
                        Code = orchMsg.Code ?? EndpointPlaceholders.InternalErrorCode,
                        Message = orchMsg.Message,
                        ReferenceId = HttpContext.TraceIdentifier,
                        IsRetryable = false,
                        Action = "Contact your administrator"
                    }, statusCode: 500)).ConfigureAwait(false);
                }
            }
            else if (result is TResponse directResponse)
            {
                Response = directResponse;
            }
        }
        catch (Exception ex)
        {
            EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            await Send.ResultAsync(TypedResults.Json(new FdwErrorResponse
            {
                Code = EndpointPlaceholders.InternalErrorCode,
                Message = "An unexpected error occurred",
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, statusCode: 500)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the business logic for this endpoint.
    /// Override this method to implement your endpoint's functionality.
    /// </summary>
    public abstract Task<object> Execute(TRequest request, CancellationToken ct);

    /// <summary>
    /// Checks authorization for the current request.
    /// Override to implement custom authorization logic.
    /// </summary>
    protected virtual Task<IGenericResult> CheckAuthorization(TRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <summary>
    /// Creates error messages from an IGenericResult for FastEndpoints.
    /// </summary>
    protected virtual string[] CreateErrorMessages(IGenericResult result)
    {
        // A failed result always carries a message (GenericResult refuses one without), so its messages are the
        // error text; there is no default message (FDW-3580).
        return [.. result.Messages.Select(m => m.Message)];
    }

}

/// <summary>
/// Base endpoint class for endpoints that don't need a request body.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class GenericEndpointBase<TResponse> : GenericEndpointBase<EmptyRequest, TResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenericEndpointBase{TResponse}"/> class.
    /// </summary>
    protected GenericEndpointBase(
        ILogger<GenericEndpointBase<EmptyRequest, TResponse>> logger)
        : base(logger)
    {
    }
}
