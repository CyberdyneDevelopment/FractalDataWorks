using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Notifications;
using Fdw.SignalR;

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
        : base(1, "Pipeline", "/hubs/pipelines", typeof(PipelineStatusHub), authorizationPolicy: null)
    {
    }

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
        => services.AddBroadcaster<
            IPipelineStatusBroadcaster,
            PipelineStatusBroadcaster,
            PipelineStatusHub,
            IPipelineStatusHubClient>(loggerFactory);

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<PipelineStatusHub>(endpoints);
}
