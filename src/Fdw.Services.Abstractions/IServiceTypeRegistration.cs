using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Collections;

/// <summary>
/// The three registration phases an option runs, and where its configuration rows live.
/// </summary>
/// <remarks>
/// <para>
/// Why this exists in Fdw.Collections rather than beside <c>IServiceType</c>: every ServiceTypeCollection
/// derives from <see cref="ServiceTypeCollectionBase{TBase,TInterface}"/>, which lives here, and the
/// collection's phase sweep has to call these members on each of its options. <c>IServiceType</c> cannot
/// move here to supply them — it depends on <c>Fdw.Abstractions</c> and <c>Fdw.Configuration</c>, and this
/// package is a leaf that those depend on, so the reference would invert.
/// </para>
/// <para>
/// So the phases are declared here as the minimum the sweep needs, and <c>IServiceType</c> extends this.
/// Nothing implements it directly — it is a contract for the collection to call through, not a second
/// abstraction for options to satisfy.
/// </para>
/// </remarks>
// Why it extends ITypeOption: Name is the option's discriminator and already lives there.
// Redeclaring it here made every member ambiguous at 250 call sites.
public interface IServiceTypeRegistration : ITypeOption
{
    /// <summary>Gets the default DataStore name for this option's configuration provider.</summary>
    string DefaultDataStoreName { get; }

    /// <summary>Gets the default path (schema) name for this option's configuration provider.</summary>
    string DefaultPathName { get; }

    /// <summary>Gets the default container (table) name for this option's configuration provider.</summary>
    string DefaultContainerName { get; }

    /// <summary>Phase 1 — binds this option's configuration.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    // Why the builder rather than (IServiceCollection, IConfiguration): it carries both, so an option
    // that needs to read IConfiguration can, while the common case just uses builder.Services. Passing
    // the narrower pair would decide for every option that it never needs anything else.
    //
    // Why the logger factory is here as well as on the other two phases: without it this phase alone
    // could not say which body it ran, and a phase that cannot report is the one whose silent failure
    // takes longest to find.
    IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null);

    /// <summary>Phase 2 — registers this option's factory and configuration provider.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="dataStoreName">Where this option's configuration rows live.</param>
    /// <param name="pathName">The schema holding this option's configuration rows.</param>
    /// <param name="containerName">The table holding this option's configuration rows.</param>
    /// <returns>The builder, for chaining.</returns>
    // Why the builder here too: Register runs before Build(), same as Configure, so an option that
    // needs IConfiguration while registering can reach it rather than being handed Services alone.
    IHostApplicationBuilder Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory,
        string dataStoreName,
        string pathName,
        string containerName);

    /// <summary>Phase 3 — post-Build initialization for this option.</summary>
    /// <param name="services">The built service provider.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The service provider, for chaining.</returns>
    IServiceProvider Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null);
}
