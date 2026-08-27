using System;
using Fdw.Results;
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
/// collection's phase collect has to call these members on each of its options. <c>IServiceType</c> cannot
/// move here to supply them — it depends on <c>Fdw.Abstractions</c> and <c>Fdw.Configuration</c>, and this
/// package is a leaf that those depend on, so the reference would invert.
/// </para>
/// <para>
/// So the phases are declared here as the minimum the collect needs, and <c>IServiceType</c> extends this.
/// Nothing implements it directly — it is a contract for the collection to call through, not a second
/// abstraction for options to satisfy.
/// </para>
/// </remarks>
// Why it extends ITypeOption: Name is the option's discriminator and already lives there.
// Redeclaring it here made every member ambiguous at 250 call sites.
public interface IServiceTypeRegistration : ITypeOption
{
    /// <summary>Gets the default DataStore name for this option's configuration provider.</summary>
    string DataStore { get; }

    /// <summary>Gets the default path (schema) name for this option's configuration provider.</summary>
    /// <remarks>
    /// Named PathName and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this string and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    string PathName { get; }

    /// <summary>Gets the default container (table) name for this option's configuration provider.</summary>
    string Container { get; }

    // ── Why every phase returns a result ────────────────────────────────────────────────────────
    // These returned the builder or the host, which left no way to say "this did not work" — so the
    // only report a failing phase could make was an exception, and an exception decides for the
    // consumer that the process ends. That is the framework taking a decision that belongs to the
    // application: a host may want to abort on a failed domain, or log it and run degraded, and it
    // can only choose if it is handed the failure instead of being unwound by it.
    //
    // The value is still carried, so chaining survives — a successful phase yields the same builder
    // or host it was given. What changes is that the caller can inspect IsSuccess first, and the
    // messages and result code that explain a failure travel with it rather than in a stack trace.

    /// <summary>Binds this option's configuration.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The builder on success; a failure carrying the reason otherwise.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    // Why the builder rather than (IServiceCollection, IConfiguration): it carries both, so an option
    // that needs to read IConfiguration can, while the common case just uses builder.Services. Passing
    // the narrower pair would decide for every option that it never needs anything else.
    //
    // Why the logger factory is here as well as on the other two phases: without it this phase alone
    // could not say which body it ran, and a phase that cannot report is the one whose silent failure
    // takes longest to find.
    IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false);

    /// <summary>Registers this option's factory and configuration provider.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The builder on success; a failure carrying the reason otherwise.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    // Why the builder here too: Register runs before Build(), same as Configure, so an option that
    // needs IConfiguration while registering can reach it rather than being handed Services alone.
    IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        bool force = false,
        bool defer = false);

    /// <summary>Post-Build initialization for this option.</summary>
    /// <param name="host">The built host. Its <c>Services</c> is the provider this phase used to take.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The host on success; a failure carrying the reason otherwise.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false);
}
