using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Says what each named alert allows the caller and what has been done to it (GET /alerts?alert=kind:id), answering <c>{ items: [...], missing: [...] }</c>
/// (FDW-2420).
/// </summary>
public abstract class ListAlertsEndpointBase : Endpoint<ListAlertsEndpointRequest, AlertListResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAlertsEndpointBase"/> class.
    /// </summary>
    /// <param name="alerts">The alert action service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected ListAlertsEndpointBase(IAlertActionService alerts, ILogger logger)
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
        Get("/alerts");
        // Why only a signed-in person: an alert is one person's, and each kind of alert says whether the caller may see one of its alerts.
        Policies("authenticated");
        Summary(s => s.Summary = "Say what alerts allow the caller");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListAlertsEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceAlertEndpoint(EndpointLogger, "list");

        if (req.Alert is not { Count: > 0 } written)
        {
            await Send.ResultAsync(MessagingFailures.Of(GenericResult.Failure(MessagingEndpointLog.AlertNamesNone(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var keys = new List<AlertKey>();
        foreach (var alert in written)
        {
            var separator = alert.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0 || !Guid.TryParse(alert.AsSpan(separator + 1), out var id))
            {
                await Send.ResultAsync(MessagingFailures.Of(GenericResult.Failure(MessagingEndpointLog.AlertWrittenWrong(EndpointLogger, alert)), HttpContext)).ConfigureAwait(false);
                return;
            }

            keys.Add(new AlertKey(alert[..separator], id));
        }

        var listed = await Alerts.List(keys, ct).ConfigureAwait(false);
        if (!listed.IsSuccess || listed.Value is not { } list)
        {
            await Send.ResultAsync(MessagingFailures.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AlertListResponse.Of(list), ct).ConfigureAwait(false);
    }
}
