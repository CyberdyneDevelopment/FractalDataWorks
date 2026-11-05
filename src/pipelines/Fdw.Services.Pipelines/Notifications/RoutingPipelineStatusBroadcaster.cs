using System;
using System.Threading.Tasks;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Configuration;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// The <see cref="IPipelineStatusBroadcaster"/> every host registers (FDW-3773). A host whose Host row says it
/// serves the real-time hubs broadcasts through SignalR, as before; any other host relays through the pipeline
/// event client to the host that does. The Host row is read through <see cref="IHostConfigurationProvider"/> when
/// an event is sent (the gateway caches it), so nothing is read at boot and no route is assumed: a missing Host
/// row, or a host that serves no hub and registers no client, is a logged failure and the event is dropped. A host that
/// serves no hub publishes only while <see cref="PipelineEventRelaySwitch"/> is on. The pipeline event client is built
/// only when the event is to be sent that way (FDW-4064): building it resolves the endpoint its connection row declares
/// and logs an error when there is none, which a host that serves the hubs or has the relay off must never be made to do.
/// </summary>
public sealed class RoutingPipelineStatusBroadcaster : IPipelineStatusBroadcaster
{
    private readonly PipelineStatusBroadcaster _hub;
    private readonly Func<IPipelineEventClient?>? _clientFactory;
    private readonly ILogger<RelayPipelineStatusBroadcaster>? _relayLogger;
    private RelayPipelineStatusBroadcaster? _relay;
    private readonly IHostConfigurationProvider _hosts;
    private readonly IHostEnvironment _environment;
    private readonly PipelineEventRelaySwitch _relaySwitch;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RoutingPipelineStatusBroadcaster"/> class.</summary>
    /// <param name="hub">The SignalR broadcaster, used on a host that serves the hubs.</param>
    /// <param name="hosts">Reads this host's Host row.</param>
    /// <param name="environment">Names this host.</param>
    /// <param name="relaySwitch">Whether a host that serves no hub relays its events.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="clientFactory">Builds the pipeline event client, called only when an event is to be relayed; answers nothing on a host with no client registered.</param>
    /// <param name="relayLogger">The logger of the relay built over the client.</param>
    public RoutingPipelineStatusBroadcaster(
        PipelineStatusBroadcaster hub,
        IHostConfigurationProvider hosts,
        IHostEnvironment environment,
        PipelineEventRelaySwitch relaySwitch,
        ILogger<RoutingPipelineStatusBroadcaster>? logger = null,
        Func<IPipelineEventClient?>? clientFactory = null,
        ILogger<RelayPipelineStatusBroadcaster>? relayLogger = null)
    {
        _hub = hub;
        _hosts = hosts;
        _environment = environment;
        _relaySwitch = relaySwitch;
        _logger = logger ?? NullLogger<RoutingPipelineStatusBroadcaster>.Instance;
        _clientFactory = clientFactory;
        _relayLogger = relayLogger;
    }

    /// <inheritdoc />
    public Task BroadcastStatusChange(string pipelineName, Guid executionId, string status, string? message = null, Guid? orgId = null)
        => Route(sink => sink.BroadcastStatusChange(pipelineName, executionId, status, message, orgId));

    /// <inheritdoc />
    public Task BroadcastProgress(string pipelineName, Guid executionId, int recordsExtracted, int recordsTransformed, int recordsLoaded, int recordsFailed, int progressPercentage, Guid? orgId = null)
        => Route(sink => sink.BroadcastProgress(pipelineName, executionId, recordsExtracted, recordsTransformed, recordsLoaded, recordsFailed, progressPercentage, orgId));

    /// <inheritdoc />
    public Task BroadcastCompletion(PipelineExecutionComplete completion, Guid? orgId = null)
        => Route(sink => sink.BroadcastCompletion(completion, orgId));

    /// <inheritdoc />
    public Task BroadcastTaskStatus(Guid executionId, Guid taskId, string status, long recordsIn, long recordsOut, long recordsDiscarded, long recordsHeld, bool sampleBufferAtCapacity)
        => Route(sink => sink.BroadcastTaskStatus(executionId, taskId, status, recordsIn, recordsOut, recordsDiscarded, recordsHeld, sampleBufferAtCapacity));

    /// <inheritdoc />
    public Task BroadcastEdgeFlow(Guid executionId, Guid sourceTaskId, Guid targetTaskId, long recordsFlowed)
        => Route(sink => sink.BroadcastEdgeFlow(executionId, sourceTaskId, targetTaskId, recordsFlowed));

    /// <inheritdoc />
    public Task BroadcastComponents(PipelineComponentsUpdate update, Guid? orgId = null)
        => Route(sink => sink.BroadcastComponents(update, orgId));

    /// <inheritdoc />
    public Task BroadcastExecutionPaused(Guid executionId)
        => Route(sink => sink.BroadcastExecutionPaused(executionId));

    /// <inheritdoc />
    public Task BroadcastExecutionResumed(Guid executionId)
        => Route(sink => sink.BroadcastExecutionResumed(executionId));

    private async Task Route(Func<IPipelineStatusBroadcaster, Task> send)
    {
        var hostName = _environment.ApplicationName;
        var host = await _hosts.Get(hostName).ConfigureAwait(false);
        if (host.IsFailure || host.Value is not HostImplementationConfiguration configured)
        {
            PipelineEventRelayLog.HostRowUnavailable(_logger, hostName);
            return;
        }

        if (configured.ServesRealTimeHubs)
        {
            await send(_hub).ConfigureAwait(false);
            return;
        }

        if (!_relaySwitch.IsOn())
        {
            return;
        }

        if (Relay() is not { } relay)
        {
            PipelineEventRelayLog.RelayClientMissing(_logger, hostName);
            return;
        }

        await send(relay).ConfigureAwait(false);
    }

    // The relay is built on the first event that is to be relayed and kept for this scope. A host with no client registered
    // answers nothing, and asks again on the next event: it is logged each time, never assumed.
    private RelayPipelineStatusBroadcaster? Relay()
    {
        if (_relay is null && _clientFactory?.Invoke() is { } client)
        {
            _relay = new RelayPipelineStatusBroadcaster(client, _relayLogger);
        }

        return _relay;
    }
}
