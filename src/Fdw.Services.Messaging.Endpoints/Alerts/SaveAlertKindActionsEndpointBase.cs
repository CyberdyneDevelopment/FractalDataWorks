using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Chooses the actions a kind of alert offers the caller's company (PUT /alerts/kinds/{Kind}/actions), replacing its choice whole and answering the kind (FDW-2430). An empty list is a choice: the kind then offers nothing.
/// </summary>
public abstract class SaveAlertKindActionsEndpointBase : Endpoint<SaveAlertKindActionsEndpointRequest, AlertKindResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveAlertKindActionsEndpointBase"/> class.
    /// </summary>
    /// <param name="kinds">The alert kind settings service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected SaveAlertKindActionsEndpointBase(IAlertKindSettingsService kinds, ILogger logger)
    {
        Kinds = kinds;
        EndpointLogger = logger;
    }

    /// <summary>Gets the alert kind settings service.</summary>
    protected IAlertKindSettingsService Kinds { get; }

    /// <summary>Gets the logger of the concrete endpoint.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Put("/alerts/kinds/{Kind}/actions");
        Policies("alerts:configure");
        Summary(s => s.Summary = "Choose the actions a kind of alert offers the company");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(SaveAlertKindActionsEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceAlertEndpoint(EndpointLogger, "choose the actions of a kind");

        var saved = await Kinds.Save(req.Kind, req.Actions, ct).ConfigureAwait(false);
        if (!saved.IsSuccess || saved.Value is not { } kind)
        {
            await Send.ResultAsync(MessagingFailures.Of(saved, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AlertKindResponse.Of(kind), ct).ConfigureAwait(false);
    }
}
