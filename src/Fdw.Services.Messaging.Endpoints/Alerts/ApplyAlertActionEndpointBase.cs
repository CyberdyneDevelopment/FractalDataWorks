using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Takes an action on an alert as the caller (POST /alerts/{Kind}/{Id}/actions/{Action}), answering the alert as it now stands (FDW-2420). Taking the same action
/// again is not an error; a timed action moves its end.
/// </summary>
public abstract class ApplyAlertActionEndpointBase : Endpoint<AlertActionEndpointRequest, AlertResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyAlertActionEndpointBase"/> class.
    /// </summary>
    /// <param name="alerts">The alert action service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected ApplyAlertActionEndpointBase(IAlertActionService alerts, ILogger logger)
    {
        Alerts = alerts;
        EndpointLogger = logger;
    }

    /// <summary>Gets the alert action service.</summary>
    protected IAlertActionService Alerts { get; }

    /// <summary>Gets the logger of the concrete endpoint.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/alerts/{Kind}/{Id}/actions/{Action}");
        // Why only a signed-in person: an alert is one person's, and each kind of alert says whether the caller may see one of its alerts.
        Policies("authenticated");
        Summary(s => s.Summary = "Take an action on an alert");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(AlertActionEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceAlertEndpoint(EndpointLogger, "apply");

        var applied = await Alerts.Apply(req.Kind, req.Id, req.Action, req.Choice, ct).ConfigureAwait(false);
        if (!applied.IsSuccess || applied.Value is not { } alert)
        {
            await Send.ResultAsync(MessagingFailures.Of(applied, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AlertResponse.Of(alert), ct).ConfigureAwait(false);
    }
}
