using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Dispatch;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.Results;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Web.RestEndpoints.Logging;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Base;

/// <summary>
/// Abstract base class for trigger-and-track endpoints.
/// Provides automatic routing, RBAC, rate limiting, execution tracking,
/// dry-run support, and optional operation dispatch.
/// </summary>
/// <typeparam name="TRequest">The trigger request type, must inherit from <see cref="TriggerOperationRequest"/>.</typeparam>
/// <remarks>
/// <para>
/// Mirrors the <see cref="Fdw.Web.RestEndpoints.Crud.CrudCreateEndpointBase{TCreateRequest, TDetail}"/>
/// pattern but for trigger-and-track workflows. Subclasses only need to specify
/// <see cref="ResourceName"/> and <see cref="ItemType"/>.
/// </para>
/// <para>
/// The trigger flow:
/// <list type="number">
/// <item><description>Generate correlation ID if not provided</description></item>
/// <item><description>If dry run, return 200 with preview info</description></item>
/// <item><description>Create execution item via <see cref="IExecutionTracker"/></description></item>
/// <item><description>Transition to Triggered state</description></item>
/// <item><description>Dispatch via <see cref="IOperationDispatcher"/> if registered</description></item>
/// <item><description>Return 201 Created with execution info</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract class TriggerEndpointBase<TRequest> : Endpoint<TRequest, TriggerOperationResponse>
    where TRequest : TriggerOperationRequest, new()
{
    /// <summary>
    /// Gets the plural resource name used for routing and policy generation (e.g., "workflows", "pipelines").
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
    /// Gets the execution item type to create when triggering (e.g., ExecutionItemTypes.Workflow).
    /// </summary>
    protected abstract IExecutionItemType ItemType { get; }

    /// <summary>
    /// Gets the execute policy for this endpoint. Defaults to "{ResourceName}:execute".
    /// </summary>
    protected virtual string ExecutePolicy => $"{ResourceName}:execute";

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}/{Name}/execute".
    /// </summary>
    protected virtual string Route => $"/{ResourceName}/{{Name}}/execute";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"Trigger {ResourceName} execution";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription =>
        $"Triggers a new {ResourceName} execution with optional parameters. Returns the execution ID for tracking.";

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ExecutePolicy);
#endif
        Throttle(hitLimit: 50, durationSeconds: 60);
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

        Description(x => x.WithTags(EndpointTag));

    ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    // Why: Trigger endpoint orchestration is inherently sequential — validate, create, transition, dispatch, respond.
    // Extracting sub-methods would not reduce logical complexity; the extra lines come from the structured error response body.
    [ConventionOverride(MaxMethodLines = 70)]
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            var tracker = Resolve<IExecutionTracker>();

            // Validate request name
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                var validationCode = OperationsResultCodes.ByName("TriggerValidationFailed");
                OnTriggerFailed(req.Name, "Name is required");
                await SendResultCodeResponse(validationCode, 400,
                    ResultDetails.Create("Reason", "Name is required"), ct).ConfigureAwait(false);
                return;
            }

            var correlationId = req.CorrelationId ?? Guid.NewGuid().ToString();
            var triggerSource = req.TriggerSource ?? "API:TriggerEndpoint";

            // Dry run: return 200 with preview info, no execution item created
            if (req.DryRun)
            {
                OnDryRun(req.Name, correlationId);
                await Send.OkAsync(new TriggerOperationResponse
                {
                    ExecutionId = Guid.Empty,
                    CorrelationId = correlationId,
                    State = "DryRun",
                    IsDryRun = true,
                    Message = $"Dry run successful. {ResourceName} '{req.Name}' would be triggered with correlation ID '{correlationId}'."
                }, ct).ConfigureAwait(false);
                return;
            }

            // Create execution item
            var createResult = await tracker.CreateItem(
                ItemType,
                req.Name,
                domainConfigurationId: null,
                correlationId: correlationId,
                triggerSource: triggerSource,
                parameters: (IReadOnlyDictionary<string, object?>?)req.Parameters,
                cancellationToken: ct).ConfigureAwait(false);

            if (!createResult.IsSuccess)
            {
                var errorMessage = createResult.CurrentMessage ?? "Unknown error";
                var failCode = OperationsResultCodes.ByName("TriggerValidationFailed");
                OnTriggerFailed(req.Name, errorMessage);
                await SendResultCodeResponse(failCode, 500,
                    ResultDetails.Create("Reason", errorMessage), ct).ConfigureAwait(false);
                return;
            }

            var execution = createResult.Value!;

            // Transition to Triggered state
            var triggerResult = await tracker.TransitionState(
                execution.Id,
                ExecutionStateTypes.Triggered,
                message: $"{ResourceName} triggered via API",
                actor: "API:TriggerEndpoint",
                cancellationToken: ct).ConfigureAwait(false);

            if (!triggerResult.IsSuccess)
            {
                OnTriggerFailed(req.Name, triggerResult.CurrentMessage ?? "Failed to transition state");
                // Still return the execution ID since it was created
            }

            // Dispatch if a dispatcher is registered
            var dispatcher = TryResolve<IOperationDispatcher>();
            if (dispatcher is not null)
            {
                var dispatchResult = await dispatcher.Dispatch(execution, ct).ConfigureAwait(false);
                if (!dispatchResult.IsSuccess)
                {
                    OnTriggerFailed(req.Name, dispatchResult.CurrentMessage ?? "Dispatch failed");
                }
            }

            OnTriggerAccepted(req.Name, execution.Id, correlationId);

            // Return 201 Created with TriggerAccepted code
            await SendTriggerResponse(execution, correlationId, ct).ConfigureAwait(false);
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
    /// Sends the 201 Created response. Override to customize (e.g., add Location header).
    /// </summary>
    protected virtual Task SendTriggerResponse(IExecutionItem execution, string correlationId, CancellationToken ct)
    {
        var acceptedCode = OperationsResultCodes.ByName("TriggerAccepted");
        return Send.ResponseAsync(new TriggerOperationResponse
        {
            ExecutionId = execution.Id,
            CorrelationId = correlationId,
            State = execution.State.Name,
            IsDryRun = false,
            Message = acceptedCode.Code
        }, 201, ct);
    }

    /// <summary>
    /// Sends an error response using an <see cref="IResultCode"/> for structured error reporting.
    /// </summary>
    /// <param name="resultCode">The operations result code describing the error.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="details">Optional result details with parameters for the error.</param>
    /// <param name="ct">The cancellation token.</param>
    protected virtual Task SendResultCodeResponse(
        IResultCode resultCode, int statusCode, IResultDetails? details, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(
            new
            {
                Code = resultCode.Code,
                Error = resultCode.MessageTemplate,
                Details = details?.Data
            }, ct);
    }

    /// <summary>
    /// Called when a dry run is requested. Override for custom logging.
    /// </summary>
    protected virtual void OnDryRun(string name, string correlationId)
    {
    }

    /// <summary>
    /// Called when the trigger fails. Override for custom logging.
    /// </summary>
    protected virtual void OnTriggerFailed(string name, string error)
    {
    }

    /// <summary>
    /// Called after the trigger is accepted and the execution item is created. Override for custom logging or post-trigger logic.
    /// </summary>
    protected virtual void OnTriggerAccepted(string name, Guid executionId, string correlationId)
    {
    }
}
