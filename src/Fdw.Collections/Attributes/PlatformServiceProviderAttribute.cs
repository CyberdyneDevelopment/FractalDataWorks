using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a hand-written three-phase class (declaring static
/// <c>Configure&lt;TBuilder&gt;(TBuilder, ILoggerFactory?)</c>, <c>Register(IServiceCollection, ILoggerFactory?)</c>,
/// and <c>Initialize(IServiceProvider, ILoggerFactory?)</c> methods) for discovery by
/// <c>PlatformServicesRegistrationGenerator</c>, so it is swept into
/// <c>Fdw.ServiceTypes.PlatformServices</c> alongside every <c>[ServiceTypeCollection]</c>-decorated class.
/// </summary>
/// <remarks>
/// Unlike <see cref="ServiceTypeCollectionAttribute"/>, this attribute triggers no TypeCollection source
/// generator — the class already declares the three phase methods by hand (e.g. a provider class such as
/// <c>DataSetProvider</c>/<c>ConfigurationGatewayDataStoreProvider</c> that isn't itself a TypeCollection). Applying this
/// attribute is purely a discovery signal for the PlatformServices sweep.
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by a source generator) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class PlatformServiceProviderAttribute : Attribute
{
    /// <summary>
    /// The category name this provider registers into <c>PlatformServices</c> under (e.g. "DataSet",
    /// "DataStore"). When absent, the generator derives it by stripping a trailing "Provider" or "Types"
    /// suffix from the class name.
    /// </summary>
    public string? ServiceCategory { get; set; }

    /// <summary>
    /// Dependency-depth layer for <c>PlatformServices</c> ordering — mirrors
    /// <see cref="ServiceTypeCollectionAttribute.Group"/>; the domain declares its own layer on itself.
    /// Default is 10 (no declared dependency on the core chain).
    /// </summary>
    public int Group { get; set; } = 10;

    /// <summary>
    /// Declares this a "declared choice" domain — mirrors <see cref="ServiceTypeCollectionAttribute.Manual"/>.
    /// When true, the PlatformServices sweeps (<c>Configure</c>/<c>Register</c>/<c>Initialize</c>) skip
    /// it and a host resolves the one configured option explicitly instead. Default is false.
    /// </summary>
    public bool Manual { get; set; }
}
