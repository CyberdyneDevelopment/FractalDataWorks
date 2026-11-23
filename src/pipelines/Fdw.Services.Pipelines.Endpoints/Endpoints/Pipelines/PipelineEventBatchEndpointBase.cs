using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for the door through which a host that serves no hub hands the pipeline events of one tick to the host that does, in one
/// request (FDW-5249). It relays each event, oldest first, through the same <see cref="PipelineEventKinds"/> the single-event door
/// (<see cref="PipelineEventEndpointBase"/>) uses and this host's own <see cref="IPipelineStatusBroadcaster"/>, so every event reaches the same hub
/// groups an in-process event would, and the hub's join rules keep deciding who sees it.
/// </summary>
/// <remarks>
/// The batch stops at the first event this host refuses, an unknown kind or a kind's missing body, and answers with a coded failure that names the
/// event's index; the events before it were broadcast. An empty batch is answered as relayed with none. There is no limit on the length of a batch
/// here: its senders are the machine identity that holds <c>pipelineevents:publish</c>, and the sender's buffer already bounds what it sends.
/// </remarks>
public abstract class PipelineEventBatchEndpointBase : Endpoint<PipelineEventBatchRequest, PipelineEventBatchResponse>
{
    private readonly IPipelineStatusBroadcaster _broadcaster;

    /// <summary>Initializes a new instance of the <see cref="PipelineEventBatchEndpointBase"/> class.</summary>
    protected PipelineEventBatchEndpointBase(IPipelineStatusBroadcaster broadcaster, ILogger logger)
    {
        _broadcaster = broadcaster;
        EndpointLogger = logger;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/pipelines/events/batch");
        Policies("pipelineevents:publish");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (summary, tags, etc.).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(PipelineEventBatchRequest req, CancellationToken ct)
    {
        var events = req.Events ?? [];
        for (var index = 0; index < events.Count; index++)
        {
            var kind = PipelineEventKinds.ByName(events[index].Kind);
            if (kind == PipelineEventKinds.NotFound)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineEventLog.BatchEventUnknownKind(EndpointLogger, index, events[index].Kind)), HttpContext)).ConfigureAwait(false);
                return;
            }

            if (!await kind.Relay(_broadcaster, events[index]).ConfigureAwait(false))
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineEventLog.BatchEventBodyMissing(EndpointLogger, index, kind.Name)), HttpContext)).ConfigureAwait(false);
                return;
            }
        }

        PipelineEventLog.BatchRelayed(EndpointLogger, events.Count);
        await Send.OkAsync(new PipelineEventBatchResponse { Relayed = events.Count }, ct).ConfigureAwait(false);
    }
}
