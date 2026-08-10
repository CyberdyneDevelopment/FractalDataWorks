using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.SignalR;

/// <summary>
/// TypeCollection of every FDW real-time SignalR hub.
/// </summary>
/// <remarks>
/// <para>
/// The source generator creates <c>All()</c>, <c>ByName()</c>, <c>ById()</c> and a static property
/// per <c>[TypeOption]</c>. Hubs declared in downstream domain assemblies are registered into this
/// collection at assembly load by the entry-point application's <c>Registration.SourceGenerators</c>
/// module initializer — exactly like every other extensible FDW TypeCollection.
/// </para>
/// <para>
/// Hosts wire the hubs through <see cref="Register"/> (before <c>Build()</c>) and
/// the host's Initialize phase (after <c>Build()</c>) — SignalR
/// registration and hub mapping are driven entirely by the collection, with no per-application
/// wiring.
/// </para>
/// <para>
/// <c>[PlatformServiceProvider]</c> marks this hand-written three-phase class for the
/// <c>PlatformServicesRegistrationGenerator</c> sweep, alongside every <c>[ServiceTypeCollection]</c>
/// domain — <see cref="Configure"/> and <see cref="Initialize"/> are no-ops declared only to
/// satisfy the required shape; <see cref="Register"/> does the only real pre-Build work this domain
/// needs. Endpoint mapping is a post-Build
/// call each host still makes manually — it is not part of this three-phase shape and is not swept.
/// </para>
/// </remarks>
[TypeCollection(typeof(RealTimeHubOptionBase), typeof(IRealTimeHub), typeof(RealTimeHubs))]
[PlatformServiceProvider(ServiceCategory = "RealTimeHubs")]
public abstract partial class RealTimeHubs : TypeCollectionBase<RealTimeHubOptionBase, IRealTimeHub>
{
    // Source generator emits the static constructor, a static property per [TypeOption] hub, and
    // All(), ByName(string), ById(int) plus the NotFound sentinel.

    /// <summary>
    /// No-op — this domain has no pre-Build IOptions binding to perform; <see cref="Register"/> does
    /// the collection's only real work. Declared only so the <c>[PlatformServiceProvider]</c> three-phase
    /// shape requirement is satisfied by ServiceTypeCollectionBase.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Unused.</param>
    /// <returns><paramref name="builder"/>, unchanged.</returns>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
        => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>
    /// Registers SignalR and every discovered real-time hub's services.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">Optional logger factory for registration logging.</param>
    /// <remarks>
    /// Call before <c>Build()</c>. Calls <c>AddSignalR()</c> once and asks each
    /// <see cref="IRealTimeHub"/> in the collection to register its own broadcaster. New hubs are
    /// picked up automatically — declaring a <c>[TypeOption(typeof(RealTimeHubs), ...)]</c> in any
    /// referenced assembly is the only step.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        // Why: NullLogger keeps registration loggable-or-silent without a conditional at each call.
        ILogger logger = loggerFactory?.CreateLogger("Fdw.SignalR") ?? NullLogger.Instance;

        services.AddSignalR();

        List<IRealTimeHub> hubs = All().ToList();
        SignalRLog.RealTimeHubsRegistering(logger, hubs.Count);

        foreach (var hub in hubs)
        {
            hub.RegisterServices(services, loggerFactory);
            SignalRLog.RealTimeHubServicesRegistered(logger, hub.Name, hub.HubType.Name, hub.Route);
        }

        SignalRLog.RealTimeHubsRegistered(logger, hubs.Count);

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// No-op — this domain has no post-Build eager-resolve step; every hub's broadcaster is registered
    /// directly in <see cref="Register"/>. Declared only so the <c>[PlatformServiceProvider]</c>
    /// three-phase shape requirement is satisfied by ServiceTypeCollectionBase.
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Unused.</param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
        => GenericResult<IHost>.Success(host);
}
