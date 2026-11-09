using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Notifications;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for the one door through which a host that serves no hub hands a pipeline event
/// to the host that does (FDW-3773). It broadcasts through this host's own
/// <see cref="IPipelineStatusBroadcaster"/>, so the event reaches the same hub groups an
/// in-process event would, and the hub's join rules keep deciding who sees it.
/// </summary>
public abstract class PipelineEventEndpointBase : Endpoint<PipelineEventRequest, PipelineEventResponse>
{
    private readonly IPipelineStatusBroadcaster _broadcaster;

    /// <summary>Initializes a new instance of the <see cref="PipelineEventEndpointBase"/> class.</summary>
    protected PipelineEventEndpointBase(IPipelineStatusBroadcaster broadcaster, ILogger logger)
    {
        _broadcaster = broadcaster;
        EndpointLogger = logger;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/pipelines/events");
        Policies("pipelineevents:publish");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (summary, tags, etc.).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(PipelineEventRequest req, CancellationToken ct)
    {
        var kind = PipelineEventKinds.ByName(req.Kind);
        if (kind == PipelineEventKinds.NotFound)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineEventLog.UnknownKind(EndpointLogger, req.Kind)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!await kind.Relay(_broadcaster, req).ConfigureAwait(false))
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineEventLog.BodyMissing(EndpointLogger, kind.Name)), HttpContext)).ConfigureAwait(false);
            return;
        }

        PipelineEventLog.Relayed(EndpointLogger, kind.Name);
        await Send.OkAsync(new PipelineEventResponse { Kind = kind.Name }, ct).ConfigureAwait(false);
    }
}
