using System.Collections.Generic;
using System.Reflection;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentOptions;

/// <summary>
/// A component collection a UI service type can drive through the three phases without naming it.
/// </summary>
/// <remarks>
/// Exists because <c>All()</c> is a generated STATIC on each derived collection, so no base and no
/// service type can call it generically.
/// </remarks>
public interface IComponentTypeCollection
{
    /// <summary>Gets or sets a value indicating whether this whole domain should be passed over.</summary>
    bool SkipRegistration { get; set; }

    /// <summary>Gets the components declared against this collection, skipped ones included.</summary>
    IEnumerable<IComponentTypeOption> Members { get; }

    /// <summary>
    /// Gets the assemblies holding the declared components.
    /// </summary>
    /// <remarks>
    /// Blazor discovers components per assembly, not per type, so a host calling
    /// <c>AddAdditionalAssemblies</c> needs this rather than the types. Distinct by construction:
    /// several components in one package yield one assembly, and Blazor throws
    /// "Assembly already defined" on a duplicate.
    /// </remarks>
    IEnumerable<Assembly> ComponentAssemblies { get; }

    /// <summary>Runs Configure for this domain, then for each component not skipped.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder);

    /// <summary>Runs Register for this domain, then for each component not skipped.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null);

    /// <summary>Runs Initialize for this domain, then for each component not skipped.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or the first failure encountered.</returns>
    IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null);
}
