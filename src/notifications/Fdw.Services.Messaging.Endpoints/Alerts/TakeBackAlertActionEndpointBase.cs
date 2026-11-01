using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Takes back one of the caller's own actions on an alert (DELETE /alerts/{Kind}/{Id}/actions/{Action}), answering the alert as it now stands (FDW-2420). An
/// action taken for everyone cannot be taken back here.
/// </summary>
public abstract class TakeBackAlertActionEndpointBase : Endpoint<AlertActionEndpointRequest, AlertResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TakeBackAlertActionEndpointBase"/> class.
    /// </summary>
    /// <param name="alerts">The alert action service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected TakeBackAlertActionEndpointBase(IAlertActionService alerts, ILogger logger)
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
        Delete("/alerts/{Kind}/{Id}/actions/{Action}");
        // Why only a signed-in person: an alert is one person's, and each kind of alert says whether the caller may see one of its alerts.
        Policies("authenticated");
        Summary(s => s.Summary = "Take back an action on an alert");
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
        MessagingEndpointLog.TraceAlertEndpoint(EndpointLogger, "take back");

        var taken = await Alerts.TakeBack(req.Kind, req.Id, req.Action, ct).ConfigureAwait(false);
        if (!taken.IsSuccess || taken.Value is not { } alert)
        {
            await Send.ResultAsync(MessagingFailures.Of(taken, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AlertResponse.Of(alert), ct).ConfigureAwait(false);
    }
}
