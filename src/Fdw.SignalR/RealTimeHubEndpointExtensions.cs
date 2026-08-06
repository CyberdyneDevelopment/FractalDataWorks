using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.SignalR;

/// <summary>
/// Endpoint-mapping phase for the <see cref="RealTimeHubs"/> collection.
/// </summary>
public static class RealTimeHubEndpointExtensions
{
    /// <summary>
    /// Maps every discovered real-time hub endpoint at its declared route.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="loggerFactory">Optional logger factory for mapping logging.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    /// <remarks>
    /// Call after <c>Build()</c>. Iterates the <see cref="RealTimeHubs"/> collection and maps each
    /// hub at the route its option declares, applying the option's authorization policy when present.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    public static IEndpointRouteBuilder MapRealTimeHubs(
        this IEndpointRouteBuilder endpoints,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        ILogger logger = loggerFactory?.CreateLogger("Fdw.SignalR") ?? NullLogger.Instance;

        List<IRealTimeHub> hubs = RealTimeHubs.All().ToList();
        SignalRLog.RealTimeHubsMapping(logger, hubs.Count);

        foreach (var hub in hubs)
        {
            hub.Map(endpoints);
            SignalRLog.RealTimeHubMapped(logger, hub.Name, hub.HubType.Name, hub.Route);
        }

        SignalRLog.RealTimeHubsMapped(logger, hubs.Count);
        return endpoints;
    }
}
