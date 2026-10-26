using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Removes the caller's company's choice for a kind of alert (DELETE /alerts/kinds/{Kind}/actions), so the kind's own list applies again, and answers the kind (FDW-2430).
/// </summary>
public abstract class ResetAlertKindActionsEndpointBase : Endpoint<AlertKindEndpointRequest, AlertKindResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetAlertKindActionsEndpointBase"/> class.
    /// </summary>
    /// <param name="kinds">The alert kind settings service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected ResetAlertKindActionsEndpointBase(IAlertKindSettingsService kinds, ILogger logger)
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
        Delete("/alerts/kinds/{Kind}/actions");
        Policies("alerts:configure");
        Summary(s => s.Summary = "Go back to the actions a kind of alert offers by default");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(AlertKindEndpointRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.TraceAlertEndpoint(EndpointLogger, "go back to the actions of a kind");

        var reset = await Kinds.Reset(req.Kind, ct).ConfigureAwait(false);
        if (!reset.IsSuccess || reset.Value is not { } kind)
        {
            await Send.ResultAsync(MessagingFailures.Of(reset, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AlertKindResponse.Of(kind), ct).ConfigureAwait(false);
    }
}
