using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Collections;

namespace Fdw.SignalR;

/// <summary>
/// CRTP base class for <see cref="IRealTimeHub"/> options.
/// </summary>
/// <remarks>
/// Concrete hub options live in their owning domain assembly and are registered against
/// <see cref="RealTimeHubs"/> via <c>[TypeOption]</c>. They supply the route and the broadcaster
/// registration; the route and authorization policy are declared (never defaulted) on construction.
/// </remarks>
public abstract class RealTimeHubOptionBase : TypeOptionBase<int, RealTimeHubOptionBase>, IRealTimeHub
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealTimeHubOptionBase"/> class.
    /// </summary>
    /// <param name="id">The unique option identifier.</param>
    /// <param name="name">The option name (must match the <c>[TypeOption]</c> attribute).</param>
    /// <param name="route">The endpoint route the hub is mapped at.</param>
    /// <param name="hubType">The concrete hub CLR type.</param>
    /// <param name="authorizationPolicy">
    /// The named authorization policy applied at mapping, or <see langword="null"/> to require the
    /// default policy (an authenticated principal). Never a path to anonymous access — authentication
    /// is mandatory for every FDW real-time hub.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="route"/> or <paramref name="hubType"/> is <see langword="null"/>.
    /// </exception>
    protected RealTimeHubOptionBase(int id, string name, string route, Type hubType, string? authorizationPolicy)
        : base(id, name)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        HubType = hubType ?? throw new ArgumentNullException(nameof(hubType));
        AuthorizationPolicy = authorizationPolicy;
    }

    /// <inheritdoc/>
    public string Route { get; }

    /// <inheritdoc/>
    public Type HubType { get; }

    /// <inheritdoc/>
    public string? AuthorizationPolicy { get; }

    /// <inheritdoc/>
    public abstract void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory);

    /// <inheritdoc/>
    public abstract void Map(IEndpointRouteBuilder endpoints);

    /// <summary>
    /// Maps <typeparamref name="THub"/> at <see cref="Route"/>, requiring authorization on the
    /// endpoint — the declared <see cref="AuthorizationPolicy"/> when one is set, otherwise the
    /// default policy (an authenticated principal).
    /// </summary>
    /// <typeparam name="THub">The concrete hub type to map.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <remarks>
    /// Derived options implement <see cref="Map"/> by calling this helper with their compile-time hub
    /// type, e.g. <c>public override void Map(IEndpointRouteBuilder e) =&gt; MapHubAt&lt;MyHub&gt;(e);</c>.
    /// Authorization is applied at the endpoint (not left to attribute discovery) so no FDW real-time
    /// hub can be mapped anonymously — authentication is mandatory (FDW-545). There is deliberately no
    /// "skip authorization" branch (NO FALLBACKS to open access).
    /// </remarks>
    protected void MapHubAt<THub>(IEndpointRouteBuilder endpoints)
        where THub : Hub
    {
        var conventions = endpoints.MapHub<THub>(Route);

        if (!string.IsNullOrEmpty(AuthorizationPolicy))
        {
            conventions.RequireAuthorization(AuthorizationPolicy);
        }
        else
        {
            conventions.RequireAuthorization();
        }
    }
}
