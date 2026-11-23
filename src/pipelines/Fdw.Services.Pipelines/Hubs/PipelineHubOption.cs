using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Collections.Attributes;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Notifications;
using Fdw.SignalR;
using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Registers the pipeline-status hub against the <see cref="RealTimeHubs"/> collection.
/// </summary>
/// <remarks>
/// Declares the route and broadcaster wiring for <see cref="PipelineStatusHub"/>. Discovered and
/// registered by the host through <see cref="RealTimeHubs.Register"/> /
/// the host's Initialize phase — there is no per-application
/// registration code.
/// </remarks>
[TypeOption(typeof(RealTimeHubs), "Pipeline")]
public sealed class PipelineHubOption : RealTimeHubOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineHubOption"/> class.
    /// </summary>
    public PipelineHubOption()
        : base("Pipeline", "/hubs/pipelines", typeof(PipelineStatusHub), authorizationPolicy: null)
    {
    }

    /// <summary>The key of the most progress broadcasts a second this hub sends for one run.</summary>
    public const string BroadcastHz = "BroadcastHz";

    /// <summary>The key of how often, in milliseconds, a host that serves no hub sends the events it has gathered to the host that does (FDW-5249).</summary>
    public const string RelayFlushMilliseconds = "RelayFlushMilliseconds";

    /// <summary>The key of how many events a host that serves no hub holds between sends before it drops its oldest components update (FDW-5249).</summary>
    public const string RelayBufferCapacity = "RelayBufferCapacity";

    private IReadOnlyDictionary<string, object>? _declaredDefaults;

    /// <inheritdoc />
    /// <remarks>
    /// The hub's coalescing rate, 5 a second (FDW-3956, Mike: "a default value set in the service type option or collection so that in the
    /// registration we can change it"). A host changes it with <c>ServiceTypeDefaults.Override(new PipelineHubOption(), BroadcastHz, n)</c>.
    /// The relay of a host that serves no hub declares how often it sends (500 ms) and how many events it holds between sends (1000) the same way
    /// (FDW-5249), and a host changes them with <c>Override</c> on <see cref="RelayFlushMilliseconds"/> and <see cref="RelayBufferCapacity"/>.
    /// </remarks>
    public override IReadOnlyDictionary<string, object> DeclaredDefaults
        => _declaredDefaults ??= ServiceTypeDefaults.With(base.DeclaredDefaults, (BroadcastHz, 5), (RelayFlushMilliseconds, 500), (RelayBufferCapacity, 1000));

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
    {
        // Why the routing broadcaster is the registered IPipelineStatusBroadcaster: this option loads into every
        // host, and only the host whose Host row says ServesRealTimeHubs can broadcast to the hub. Every other
        // host relays to it (FDW-3773). The SignalR broadcaster stays registered as its own type for the routing
        // broadcaster to use.
        services.AddBroadcaster<
            PipelineStatusBroadcaster,
            PipelineStatusBroadcaster,
            PipelineStatusHub,
            IPipelineStatusHubClient>(loggerFactory);
        services.AddSingleton<PipelineEventRelaySwitch>();
        // A host that serves no hub sends the events it has gathered once a tick, in one request, instead of one request per event (FDW-5249).
        // Every host registers these; on the host that serves the hubs the buffer is never filled and the flusher finds nothing to send.
        services.AddSingleton(sp => PipelineEventRelayTuning.Read((sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance).CreateLogger<PipelineEventRelayTuning>()));
        services.AddSingleton(sp => new PipelineEventRelayBuffer(sp.GetRequiredService<PipelineEventRelayTuning>().BufferCapacity, sp.GetService<ILogger<PipelineEventRelayBuffer>>()));
        services.AddHostedService<PipelineEventRelayFlusher>();
        // The client is handed over as a factory: resolving the batch client builds the HTTP client and logs an error when no connection
        // row declares its endpoint, and a host that serves the hubs, or has the relay off, must not be made to do that (FDW-4064). What the
        // relay sends through is the buffering client, so the events leave in batches (FDW-5249); a host with no batch client has no relay.
        services.AddScoped<IPipelineStatusBroadcaster>(sp => new RoutingPipelineStatusBroadcaster(
            sp.GetRequiredService<PipelineStatusBroadcaster>(),
            sp.GetRequiredService<IHostConfigurationProvider>(),
            sp.GetRequiredService<IHostEnvironment>(),
            sp.GetRequiredService<PipelineEventRelaySwitch>(),
            sp.GetService<ILogger<RoutingPipelineStatusBroadcaster>>(),
            () => sp.GetService<IPipelineEventBatchClient>() is null ? null : new BufferedPipelineEventClient(sp.GetRequiredService<PipelineEventRelayBuffer>()),
            sp.GetService<ILogger<RelayPipelineStatusBroadcaster>>()));
    }

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<PipelineStatusHub>(endpoints);
}
