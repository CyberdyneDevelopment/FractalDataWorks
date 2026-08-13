using System;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes.Results;
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
    protected static Guid DeriveId(string name) => OptionId.Derive(name);
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
    public string DataStore { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Named PathName and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this string and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathName { get; }

    /// <inheritdoc />
    public string Container { get; }

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

    /// <summary>Gets a value indicating whether Configure has run.</summary>
    /// <remarks>
    /// A phase runs once. Idempotence is what makes chaining safe: a body appended by one contributor
    /// cannot re-run what an earlier one already did, however many times a phase is invoked.
    /// </remarks>
    public bool Configured { get; private set; }

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public bool Registered { get; private set; }

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public bool Initialized { get; private set; }

    /// <summary>Gets or sets a value indicating whether this option is switched off.</summary>
    /// <remarks>
    /// Checked by the option itself, not only by the collection cycling it — calling a phase directly
    /// must honour the switch too, or the switch means nothing to half its callers.
    /// </remarks>
    public bool SkipRegistration { get; set; }
    /// <summary>Gets or sets a value indicating whether Configure is switched off.</summary>
    /// <remarks>
    /// One flag per phase, because they are switched off for different reasons: a domain may
    /// need its services registered while its post-Build wiring is suppressed, and a single flag
    /// named for one phase silently governing the other two says something false about what it does.
    /// </remarks>
    public bool SkipConfiguration { get; set; }

    /// <summary>Gets or sets a value indicating whether Initialize is switched off.</summary>
    public bool SkipInitialization { get; set; }

    /// <summary>Gets this option's Configure body.</summary>
    protected Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> ConfigurationMethod { get; private set; }
        = static builder => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Gets this option's Register body.</summary>
    protected Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> RegistrationMethod { get; private set; }
        = static (builder, loggerFactory) => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Gets this option's Initialize body.</summary>
    /// <remarks>
    /// Why the host and not its <see cref="IServiceProvider"/>: an option whose initialization needs
    /// something the host owns — a middleware stage, an endpoint route — could not say so through a
    /// bare provider, because the host is not resolvable from the container it built. It had to be
    /// wired by hand at the composition root instead, far from the option that required it, where
    /// nothing connects the two. Taking the host means the requirement is stated by the option that
    /// has it. <c>host.Services</c> is the same provider this used to receive.
    /// </remarks>
    protected Func<IHost, ILoggerFactory?, IGenericResult<IHost>> InitializationMethod { get; private set; }
        = static (host, loggerFactory) => GenericResult<IHost>.Success(host);

    // ── Which body is installed ─────────────────────────────────────────────────────────────────
    // Set by the gerund setters, read by the invokers, so each phase can say at Info whether the
    // do-nothing default above or a body this option supplied is the one about to run.
    //
    // Why it is worth saying: an option that never sets a phase body is indistinguishable, from
    // outside, from one whose body ran and did nothing. When a service fails to resolve later, that
    // is the first fact worth having and the hardest to recover after the fact.



    /// <summary>Sets this option's Configure body.</summary>
    /// <param name="method">The replacement delegate.</param>
    public void Configuration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Configure", nameof(method));
            return;
        }

        ConfigurationMethod = method;
    }

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>
    /// Prefer this to <see cref="Configuration"/>. Replacing discards the base's own body along with
    /// anything another contributor added, and nothing reports that it happened — the option simply
    /// stops doing part of its job. Appending cannot lose work, so the guarantee does not rest on a
    /// caller remembering to capture what was there first.
    /// </remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendConfiguration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Configure", nameof(method));
            return;
        }

        var existing = ConfigurationMethod;
        ConfigurationMethod = (builder) =>
        {
            var result = existing(builder);
            return result.IsFailure ? result : method(builder);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public void PrependConfiguration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Configure", nameof(method));
            return;
        }

        var existing = ConfigurationMethod;
        ConfigurationMethod = (builder) =>
        {
            var result = method(builder);
            return result.IsFailure ? result : existing(builder);
        };
    }

    /// <summary>Sets this option's Register body.</summary>
    /// <param name="method">The replacement delegate.</param>
    public void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Register", nameof(method));
            return;
        }

        RegistrationMethod = method;
    }

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>
    /// Prefer this to <see cref="Registration"/>. Replacing discards the base's own body along with
    /// anything another contributor added, and nothing reports that it happened — the option simply
    /// stops doing part of its job. Appending cannot lose work, so the guarantee does not rest on a
    /// caller remembering to capture what was there first.
    /// </remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Register", nameof(method));
            return;
        }

        var existing = RegistrationMethod;
        RegistrationMethod = (builder, loggerFactory) =>
        {
            var result = existing(builder, loggerFactory);
            return result.IsFailure ? result : method(builder, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public void PrependRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Register", nameof(method));
            return;
        }

        var existing = RegistrationMethod;
        RegistrationMethod = (builder, loggerFactory) =>
        {
            var result = method(builder, loggerFactory);
            return result.IsFailure ? result : existing(builder, loggerFactory);
        };
    }

    /// <summary>Sets this option's Initialize body.</summary>
    /// <param name="method">The replacement delegate.</param>
    public void Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Initialize", nameof(method));
            return;
        }

        InitializationMethod = method;
    }

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>
    /// Prefer this to <see cref="Initialization"/>. Replacing discards the base's own body along with
    /// anything another contributor added, and nothing reports that it happened — the option simply
    /// stops doing part of its job. Appending cannot lose work, so the guarantee does not rest on a
    /// caller remembering to capture what was there first.
    /// </remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Initialize", nameof(method));
            return;
        }

        var existing = InitializationMethod;
        InitializationMethod = (host, loggerFactory) =>
        {
            var result = existing(host, loggerFactory);
            return result.IsFailure ? result : method(host, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public void PrependInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Initialize", nameof(method));
            return;
        }

        var existing = InitializationMethod;
        InitializationMethod = (host, loggerFactory) =>
        {
            var result = method(host, loggerFactory);
            return result.IsFailure ? result : existing(host, loggerFactory);
        };
    }

    // Why none of these is virtual: an override is invisible to the chain, which invokes the func a
    // level holds. The reason one used to be needed — a base contributing wiring that a derived
    // Registration(...) would clobber — is what Append and Prepend remove.

    /// <inheritdoc />
    /// <remarks>
    /// The collection that sweeps this option — <c>ConnectionTypes</c> and the rest — is written by
    /// <c>ServiceTypeCollectionGenerator</c> from the <c>[ServiceTypeCollection]</c> attribute, not by
    /// hand. It is that generated sweep which calls this, in the order the log line reports.
    /// </remarks>
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        if (!force && (Configured || SkipConfiguration))
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = RunPhase(loggerFactory, "Configure", ServiceTypePhaseSequence.Configure,
            () => ConfigurationMethod(builder));
        Configured = true;
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Called by the generated collection's phase-2 sweep. Where the collection declares a
    /// <c>ProviderType</c>, the generated part registers that provider independently of this call.
    /// </remarks>
    public IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        bool force = false)
    {
        if (!force && (Registered || SkipRegistration))
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = RunPhase(loggerFactory, "Register", ServiceTypePhaseSequence.Register,
            () => RegistrationMethod(builder, loggerFactory));
        Registered = true;
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Called by the generated collection's phase-3 sweep, after the host has been built.
    /// </remarks>
    public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        if (!force && (Initialized || SkipInitialization))
        {
            return GenericResult<IHost>.Success(host);
        }

        var result = RunPhase(loggerFactory, "Initialize", ServiceTypePhaseSequence.Initialize,
            () => InitializationMethod(host, loggerFactory));
        Initialized = true;
        return result;
    }

    // Why the body arrives as a thunk rather than the func itself: the three phases take different
    // arguments, and closing over them here keeps one logging contract instead of three that drift.
    //
    // Why catch-log-return rather than log-and-rethrow: an exception decides for the application that
    // the process ends. A framework does not get to make that call — the host may want to abort on a
    // failed domain or run without it, and it can only choose if the failure arrives as a value. The
    // catch is the boundary where an option that still throws is converted into the result everything
    // above this expects, so one badly-behaved option cannot unwind a sweep that was handling failures.
    //
    // A body that returns a failure is passed through untouched: it already carries its own domain's
    // code, which is more specific than anything this could substitute. Only the throw needs a code.
    private IGenericResult<T> RunPhase<T>(
        ILoggerFactory? loggerFactory,
        string phase,
        ServiceTypePhaseSequence sequence,
        Func<IGenericResult<T>> body)
    {
        var logger = loggerFactory?.CreateLogger(GetType().FullName ?? Name) ?? (ILogger)NullLogger.Instance;
        var ordinal = sequence.NextOption();
        // Why one message rather than a custom/default split: a body that has been appended to is
        // neither, so the distinction stopped describing anything real.
        ServiceTypeLog.OptionPhaseCustom(logger, Name, phase, ordinal, sequence.CurrentCollectionName, ServiceTypeLog.PhaseDocumentation);

        try
        {
            var result = body();

            if (result.IsSuccess)
                ServiceTypeLog.OptionPhaseSucceeded(logger, Name, phase, ordinal, sequence.CurrentCollectionName);
            else
                ServiceTypeLog.OptionPhaseReportedFailure(
                    logger, Name, phase, ordinal, sequence.CurrentCollectionName, result.CurrentMessage ?? string.Empty);

            return result;
        }
        catch (Exception ex)
        {
            ServiceTypeLog.OptionPhaseFailed(logger, ex, Name, phase, ordinal, sequence.CurrentCollectionName, "chained");
            return GenericResult<T>.Failure(
                ServiceTypeResultCodes.ByName("OptionPhaseFailed"),
                ResultDetails.Create("OptionName", Name)
                    .With("Phase", phase)
                    .With("Ordinal", ordinal)
                    .With("CollectionName", sequence.CurrentCollectionName)
                                        .With("ErrorMessage", ex.Message));
        }
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
        DataStore = defaultDataStoreName;
        PathName = defaultPathName;
        Container = defaultContainerName;
    }
}
