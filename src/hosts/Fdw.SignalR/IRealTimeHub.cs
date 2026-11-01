using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Collections;

namespace Fdw.SignalR;

/// <summary>
/// A discoverable descriptor for a single FDW real-time SignalR hub.
/// </summary>
/// <remarks>
/// Each hub in the framework is declared as a <c>[TypeOption(typeof(RealTimeHubs), "Name")]</c> in
/// its owning domain assembly. The <see cref="RealTimeHubs"/> collection enumerates every option so
/// the host can register the hubs' services and map their endpoints without a hand-maintained
/// per-application extension method.
/// </remarks>
public interface IRealTimeHub : ITypeOption<int, IRealTimeHub>
{
    /// <summary>
    /// Gets the endpoint route the hub is mapped at (for example <c>"/hubs/pipelines"</c>).
    /// </summary>
    string Route { get; }

    /// <summary>
    /// Gets the concrete <see cref="Microsoft.AspNetCore.SignalR.Hub"/> CLR type, used for diagnostics.
    /// </summary>
    Type HubType { get; }

    /// <summary>
    /// Gets the named authorization policy applied when the hub endpoint is mapped, or
    /// <see langword="null"/> to require the default policy (an authenticated principal).
    /// Authentication is always required — this is never a path to anonymous access.
    /// </summary>
    string? AuthorizationPolicy { get; }

    /// <summary>
    /// Registers the broadcaster and any hub-scoped services this hub requires.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory);

    /// <summary>
    /// Maps the hub endpoint at <see cref="Route"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    void Map(IEndpointRouteBuilder endpoints);
}
