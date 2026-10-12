using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections;

/// <summary>
/// Marks a hand-written three-phase class (declaring static
/// <c>Configure&lt;TBuilder&gt;(TBuilder, ILoggerFactory?)</c>, <c>Register(IServiceCollection, ILoggerFactory?)</c>,
/// and <c>Initialize(IServiceProvider, ILoggerFactory?)</c> methods) for discovery by
/// <c>PlatformServicesRegistrationGenerator</c>, so it is collected into
/// <c>Fdw.ServiceTypes.PlatformServices</c> alongside every <c>[ServiceTypeCollection]</c>-decorated class.
/// </summary>
/// <remarks>
/// Unlike <see cref="ServiceTypeCollectionAttribute"/>, this attribute triggers no TypeCollection source
/// generator — the class already declares the three phase methods by hand (e.g. a provider class such as
/// <c>DataSetProvider</c>/<c>ConfigurationGatewayDataStoreProvider</c> that isn't itself a TypeCollection). Applying this
/// attribute is purely a discovery signal for the PlatformServices collect.
/// </remarks>
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


}
