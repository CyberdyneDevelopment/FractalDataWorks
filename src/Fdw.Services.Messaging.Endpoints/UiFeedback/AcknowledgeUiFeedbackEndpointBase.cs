using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Acknowledges a UI comment as the caller (POST /ui-feedback/{Id}/acknowledge), answering the comment with acknowledgedByMe set. It is the Acknowledge action of the
/// comment's alert (FDW-2420), the same as POST /alerts/UiFeedback/{Id}/actions/Acknowledge. Acknowledging again changes nothing.
/// </summary>
public abstract class AcknowledgeUiFeedbackEndpointBase : Endpoint<UiFeedbackIdRequest, UiFeedbackResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcknowledgeUiFeedbackEndpointBase"/> class.
    /// </summary>
    /// <param name="feedback">The UI comment service.</param>
    /// <param name="alerts">The alert action service, which records the acknowledgement.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected AcknowledgeUiFeedbackEndpointBase(IUiFeedbackService feedback, IAlertActionService alerts, ILogger logger)
    {
        Feedback = feedback;
        Alerts = alerts;
        EndpointLogger = logger;
    }

    /// <summary>Gets the UI comment service.</summary>
    protected IUiFeedbackService Feedback { get; }

    /// <summary>Gets the alert action service.</summary>
    protected IAlertActionService Alerts { get; }

    /// <summary>Gets the logger of the concrete endpoint.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/ui-feedback/{Id}/acknowledge");
        Policies("ui-feedback:read");
        Summary(s => s.Summary = "Acknowledge a UI comment");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(UiFeedbackIdRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceUiFeedbackEndpoint(EndpointLogger, "acknowledge");

        var acknowledged = await Alerts.Apply(UiFeedbackAlert.Kind, req.Id, UiFeedbackAlert.AcknowledgeAction, null, ct).ConfigureAwait(false);
        if (!acknowledged.IsSuccess)
        {
            await Send.ResultAsync(MessagingFailures.Of(acknowledged, HttpContext)).ConfigureAwait(false);
            return;
        }

        var opened = await Feedback.Get(req.Id, ct).ConfigureAwait(false);
        if (!opened.IsSuccess || opened.Value is not { } comment)
        {
            await Send.ResultAsync(MessagingFailures.Of(opened, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(UiFeedbackResponse.Of(comment), ct).ConfigureAwait(false);
    }
}
