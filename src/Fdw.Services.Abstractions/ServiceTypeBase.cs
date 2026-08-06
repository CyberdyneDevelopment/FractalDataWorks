using System;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.ServiceTypes;

/// <summary>
/// Base class for every service type option. A service type declares what its kind IS — the service,
/// factory and configuration types — and carries the three methods that register it.
/// </summary>
/// <typeparam name="TService">The service interface or class type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating service instances.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the service.</typeparam>
/// <remarks>
/// Three generic parameters, and no tier above or below them: a provider fills its own factory
/// registry, so an option never needs a typed provider parameter to hand one to.
/// </remarks>
public abstract class ServiceTypeBase<TService, TFactory, TConfiguration>
    : TypeOptionBase<Guid, IServiceType<Guid>>,
      IServiceType<Guid, TService, TFactory, TConfiguration>,
      IServiceType
    where TService : IGenericService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IServiceFactory<TService, TConfiguration>
{
    /// <summary>
    /// Derives this service type's identifier from its name.
    /// </summary>
    /// <param name="name">The option's name — its discriminator within the collection.</param>
    /// <returns>A stable identifier for an option of that name.</returns>
    /// <remarks>
    /// <para>
    /// Identity comes from the option, not from the option's generic arguments. The arguments do not
    /// identify anything: a domain's options routinely close this base identically — every option in
    /// <c>SessionStateTypes</c> is a
    /// <c>ServiceTypeBase&lt;IGenericService, ISessionStateServiceFactory, IServiceConfiguration&gt;</c> —
    /// and an id computed from them is one value shared by the whole domain. Because the id is what
    /// <c>ServiceTypeCollectionBase.RegisterMember</c> keys membership on, every option after the first
    /// then looked like a duplicate and was dropped without a word.
    /// </para>
    /// <para>
    /// The name is the right source because it is already the thing that distinguishes options within a
    /// collection: it is what <c>[ServiceTypeOption(..., "MsSql")]</c> declares, what <c>ByName</c>
    /// resolves, and what the generated metadata already hashes. Two options in one collection cannot
    /// share a name without colliding in that lookup first.
    /// </para>
    /// </remarks>
    // Why MD5: deterministic hashing for a stable id, not security.
#pragma warning disable CA5351, SCS0006, CA1850
    protected static Guid DeriveId(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        using var md5 = System.Security.Cryptography.MD5.Create();
        return new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name)));
    }
#pragma warning restore CA5351, SCS0006, CA1850

    /// <summary>Gets the name of this service type — its discriminator within the collection.</summary>
    [TypeLookup("ByName")]
    public new string Name => base.Name;

    /// <summary>Gets the service interface implementations must satisfy.</summary>
    // Not a [TypeLookup]: every option in a collection shares one ServiceType, so a 1:1 lookup collides.
    public Type ServiceType => typeof(TService);

    /// <summary>Gets the factory type for creating service instances.</summary>
    public Type FactoryType => typeof(TFactory);

    /// <summary>Gets the configuration type this service reads.</summary>
    public Type ConfigurationType => typeof(TConfiguration);

    /// <summary>Gets the configuration section name for appsettings.json.</summary>
    public string SectionName => ConfigurationKey;

    /// <inheritdoc />
    public string DefaultDataStoreName { get; }

    /// <inheritdoc />
    public string DefaultPathName { get; }

    /// <inheritdoc />
    public string DefaultContainerName { get; }

    // ── The three registration methods ──────────────────────────────────────────────────────────
    // Each one is a func holding the body, a gerund that sets it, and a method that invokes it. The
    // author of a service type sets its funcs in the constructor; a host can replace one afterwards.
    //
    // Defaults are set HERE, in the declaration, so a func is never null and the invoker never guards.
    //
    // Why funcs rather than virtual methods to override: there is exactly one body per option, and it
    // can be replaced at runtime by whoever composes the app without subclassing. The shape this
    // replaced needed a virtual, an override, a nullable override field and an Invoke wrapper that
    // checked the field before falling through to virtual dispatch — four moving parts for one body.

    /// <summary>Gets this option's Configure body.</summary>
    protected Func<IHostApplicationBuilder, IHostApplicationBuilder> ConfigurationMethod { get; private set; }
        = static builder => builder;

    /// <summary>Gets this option's Register body.</summary>
    protected Func<IHostApplicationBuilder, ILoggerFactory?, string, string, string, IHostApplicationBuilder> RegistrationMethod { get; private set; }
        = static (builder, loggerFactory, dataStoreName, pathName, containerName) => builder;

    /// <summary>Gets this option's Initialize body.</summary>
    protected Func<IServiceProvider, ILoggerFactory?, IServiceProvider> InitializationMethod { get; private set; }
        = static (services, loggerFactory) => services;

    // ── Which body is installed ─────────────────────────────────────────────────────────────────
    // Set by the gerund setters, read by the invokers, so each phase can say at Info whether the
    // do-nothing default above or a body this option supplied is the one about to run.
    //
    // Why it is worth saying: an option that never sets a phase body is indistinguishable, from
    // outside, from one whose body ran and did nothing. When a service fails to resolve later, that
    // is the first fact worth having and the hardest to recover after the fact.

    /// <summary>Gets a value indicating whether this option supplied its own Configure body.</summary>
    protected bool ConfigurationIsCustom { get; private set; }

    /// <summary>Gets a value indicating whether this option supplied its own Register body.</summary>
    protected bool RegistrationIsCustom { get; private set; }

    /// <summary>Gets a value indicating whether this option supplied its own Initialize body.</summary>
    protected bool InitializationIsCustom { get; private set; }

    /// <summary>Sets this option's Configure body.</summary>
    /// <param name="method">The replacement delegate.</param>
    public void Configuration(Func<IHostApplicationBuilder, IHostApplicationBuilder> method)
    {
        ConfigurationMethod = method ?? throw new ArgumentNullException(nameof(method));
        ConfigurationIsCustom = true;
    }

    /// <summary>Sets this option's Register body.</summary>
    /// <param name="method">The replacement delegate.</param>
    public void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, string, string, string, IHostApplicationBuilder> method)
    {
        RegistrationMethod = method ?? throw new ArgumentNullException(nameof(method));
        RegistrationIsCustom = true;
    }

    /// <summary>Sets this option's Initialize body.</summary>
    /// <param name="method">The replacement delegate.</param>
    public void Initialization(Func<IServiceProvider, ILoggerFactory?, IServiceProvider> method)
    {
        InitializationMethod = method ?? throw new ArgumentNullException(nameof(method));
        InitializationIsCustom = true;
    }

    // Why these three are virtual: the gerund setters REPLACE a body, which is the intended semantic
    // for an option customizing its own phase. But it makes a base class unable to contribute wiring
    // that must always run — a base that calls Registration(...) in its constructor is silently
    // clobbered when the derived constructor calls Registration(...) afterwards, and the base's
    // registrations simply never happen. Overriding the INVOKER is the sanctioned way to add
    // invariant, non-overridable wiring: do the base's work, then delegate to base.Xxx(...) so the
    // option's own func still runs.

    /// <inheritdoc />
    /// <remarks>
    /// The collection that sweeps this option — <c>ConnectionTypes</c> and the rest — is written by
    /// <c>ServiceTypeCollectionGenerator</c> from the <c>[ServiceTypeCollection]</c> attribute, not by
    /// hand. It is that generated sweep which calls this, in the order the log line reports.
    /// </remarks>
    public virtual IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
        => RunPhase(loggerFactory, "Configure", ConfigurationIsCustom, ServiceTypePhaseSequence.Configure,
            () => ConfigurationMethod(builder));

    /// <inheritdoc />
    /// <remarks>
    /// Called by the generated collection's phase-2 sweep. Where the collection declares a
    /// <c>ProviderType</c>, the generated part registers that provider independently of this call.
    /// </remarks>
    public virtual IHostApplicationBuilder Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory,
        string dataStoreName,
        string pathName,
        string containerName)
        => RunPhase(loggerFactory, "Register", RegistrationIsCustom, ServiceTypePhaseSequence.Register,
            () => RegistrationMethod(builder, loggerFactory, dataStoreName, pathName, containerName));

    /// <inheritdoc />
    /// <remarks>
    /// Called by the generated collection's phase-3 sweep, after the host has been built.
    /// </remarks>
    public virtual IServiceProvider Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null)
        => RunPhase(loggerFactory, "Initialize", InitializationIsCustom, ServiceTypePhaseSequence.Initialize,
            () => InitializationMethod(services, loggerFactory));

    // Why the body arrives as a thunk rather than the func itself: the three phases take different
    // arguments, and closing over them here keeps one logging contract instead of three that drift.
    //
    // Why log-and-rethrow rather than catch-log-return: these return the builder or the provider, so
    // there is no failure value to hand back. Swallowing would let a half-registered option reach a
    // running application. The log names the option, its position, and which body was running.
    private T RunPhase<T>(
        ILoggerFactory? loggerFactory,
        string phase,
        bool isCustom,
        ServiceTypePhaseSequence sequence,
        Func<T> body)
    {
        var logger = loggerFactory?.CreateLogger(GetType().FullName ?? Name) ?? (ILogger)NullLogger.Instance;
        var ordinal = sequence.NextOption();

        if (isCustom)
            ServiceTypeLog.OptionPhaseCustom(logger, Name, phase, ordinal, sequence.CurrentCollectionName, ServiceTypeLog.PhaseDocumentation);
        else
            ServiceTypeLog.OptionPhaseDefault(logger, Name, phase, ordinal, sequence.CurrentCollectionName, ServiceTypeLog.PhaseDocumentation);

        try
        {
            var result = body();
            ServiceTypeLog.OptionPhaseSucceeded(logger, Name, phase, ordinal, sequence.CurrentCollectionName);
            return result;
        }
        catch (Exception ex)
        {
            ServiceTypeLog.OptionPhaseFailed(logger, ex, Name, phase, ordinal, sequence.CurrentCollectionName, isCustom ? "custom" : "default");
            throw;
        }
    }

    /// <summary>
    /// Binds this service type's configuration section from appsettings.json.
    /// </summary>
    /// <param name="services">The service collection to bind against.</param>
    protected void RegisterConfiguration(IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddOptions<TConfiguration>()
            .BindConfiguration(SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeBase{TService, TFactory, TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The name of this service type.</param>
    /// <param name="sectionName">The configuration section name for appsettings.json.</param>
    /// <param name="displayName">The display name for this service type.</param>
    /// <param name="description">The description of what this service type provides.</param>
    /// <param name="category">The category of the service type.</param>
    /// <param name="defaultDataStoreName">The default DataStore name for this type's configuration provider.</param>
    /// <param name="defaultPathName">The default path (schema) name for this type's configuration provider.</param>
    /// <param name="defaultContainerName">The default container (table) name for this type's configuration command.</param>
    protected ServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null,
        string defaultDataStoreName = "",
        string defaultPathName = "",
        string defaultContainerName = "")
        : base(DeriveId(name), name, sectionName, displayName, description, category)
    {
        DefaultDataStoreName = defaultDataStoreName;
        DefaultPathName = defaultPathName;
        DefaultContainerName = defaultContainerName;
    }
}
