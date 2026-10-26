using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Says, for every kind of alert, the actions it offers by default and the actions it offers the caller's company (GET /alerts/kinds), answering <c>{ kinds: [...] }</c> (FDW-2430).
/// </summary>
public abstract class ListAlertKindsEndpointBase : EndpointWithoutRequest<AlertKindListResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAlertKindsEndpointBase"/> class.
    /// </summary>
    /// <param name="kinds">The alert kind settings service.</param>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    protected ListAlertKindsEndpointBase(IAlertKindSettingsService kinds, ILogger logger)
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
        Get("/alerts/kinds");
        Policies("alerts:configure");
        Summary(s => s.Summary = "Say which actions each kind of alert offers the company");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup, such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        MessagingEndpointLog.TraceAlertEndpoint(EndpointLogger, "list kinds");

        var listed = await Kinds.List(ct).ConfigureAwait(false);
        if (!listed.IsSuccess || listed.Value is not { } kinds)
        {
            await Send.ResultAsync(MessagingFailures.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AlertKindListResponse.Of(kinds), ct).ConfigureAwait(false);
    }
}
