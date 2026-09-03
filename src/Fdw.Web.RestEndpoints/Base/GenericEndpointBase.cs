using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Orchestration.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Web.RestEndpoints.Logging;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;

namespace Fdw.Web.RestEndpoints.Base;

/// <summary>
/// Base endpoint class that provides authentication, RBAC, and Fdw service integration.
/// Inherits from FastEndpoints for natural HTTP handling and adds Fdw service injection.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class GenericEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericEndpointBase{TRequest, TResponse}"/> class.
    /// </summary>
    protected GenericEndpointBase(
        ILogger<GenericEndpointBase<TRequest, TResponse>> logger,
        IDataGatewayProvider dataGateways,
        IOrchestrationExecutor? executor = null,
        ISchedulingService? scheduler = null)
    {
        Logger = logger;
        _dataGateways = dataGateways;
        Executor = executor;
        Scheduler = scheduler;
    }

    /// <summary>
    /// Gets the logger instance for this endpoint.
    /// </summary>
    protected new ILogger Logger { get; }

    /// <summary>
    /// Gets the data gateway for database operations.
    /// </summary>
    protected IDataGateway DataGateway => _dataGateways.ByName("Main");

    /// <summary>
    /// Gets the orchestration executor for running orchestrations. Null if not registered.
    /// </summary>
    protected IOrchestrationExecutor? Executor { get; }

    /// <summary>
    /// Gets the scheduling service for scheduling tasks. Null if not registered.
    /// </summary>
    protected ISchedulingService? Scheduler { get; }

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
                await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
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
                    HttpContext.Response.StatusCode = 500;
                    HttpContext.Response.ContentType = "application/json";
                    await HttpContext.Response.WriteAsJsonAsync(new FdwErrorResponse
                    {
                        Code = orchMsg.Code ?? "INTERNAL_ERROR",
                        Message = orchMsg.Message,
                        ReferenceId = HttpContext.TraceIdentifier,
                        IsRetryable = false,
                        Action = "Contact your administrator"
                    }, ct).ConfigureAwait(false);
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
            HttpContext.Response.StatusCode = 500;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new FdwErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred",
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, ct).ConfigureAwait(false);
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
        var messages = new List<string>();

        if (result.Messages.Count > 0)
            messages.AddRange(result.Messages.Select(m => m.Message));
        else if (!string.IsNullOrEmpty(result.CurrentMessage))
            messages.Add(result.CurrentMessage);

        return messages.Count > 0 ? messages.ToArray() : ["An error occurred"];
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
        ILogger<GenericEndpointBase<EmptyRequest, TResponse>> logger,
        IDataGatewayProvider dataGateways,
        IOrchestrationExecutor? executor = null,
        ISchedulingService? scheduler = null)
        : base(logger, dataGateways, executor, scheduler)
    {
    }
}
