using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    private IReadOnlyDictionary<string, object>? _declaredDefaults;

    /// <inheritdoc />
    /// <remarks>
    /// The hub's coalescing rate, 5 a second (FDW-3956, Mike: "a default value set in the service type option or collection so that in the
    /// registration we can change it"). A host changes it with <c>ServiceTypeDefaults.Override(new PipelineHubOption(), BroadcastHz, n)</c>.
    /// </remarks>
    public override IReadOnlyDictionary<string, object> DeclaredDefaults
        => _declaredDefaults ??= ServiceTypeDefaults.With(base.DeclaredDefaults, (BroadcastHz, 5));

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
        // The client is handed over as a factory: resolving it builds the HTTP client and logs an error when no connection
        // row declares its endpoint, and a host that serves the hubs, or has the relay off, must not be made to do that (FDW-4064).
        services.AddScoped<IPipelineStatusBroadcaster>(sp => new RoutingPipelineStatusBroadcaster(
            sp.GetRequiredService<PipelineStatusBroadcaster>(),
            sp.GetRequiredService<IHostConfigurationProvider>(),
            sp.GetRequiredService<IHostEnvironment>(),
            sp.GetRequiredService<PipelineEventRelaySwitch>(),
            sp.GetService<ILogger<RoutingPipelineStatusBroadcaster>>(),
            () => sp.GetService<IPipelineEventClient>(),
            sp.GetService<ILogger<RelayPipelineStatusBroadcaster>>()));
    }

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<PipelineStatusHub>(endpoints);
}
