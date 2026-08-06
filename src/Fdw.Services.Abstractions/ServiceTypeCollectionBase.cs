using System;
using System.Collections.Generic;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Collections;

/// <summary>
/// Base class for ServiceType collections: the registry of a domain's options, and the three
/// registration phases over them.
/// </summary>
/// <typeparam name="TBase">The base type for all options in this collection.</typeparam>
/// <typeparam name="TInterface">The interface that all options implement.</typeparam>
/// <remarks>
/// The registry itself is typed to <see cref="IServiceTypeRegistration"/>, not <typeparamref name="TInterface"/>:
/// an option implements its collection's NON-generic marker (e.g. <c>IConnectionType</c>) plus its own
/// closed generic form, so requiring the collection's generic TInterface here would reject every option.
/// The generated lookups cast Options to TInterface where the typed view is actually needed.
/// </remarks>
/// <remarks>
/// <para>
/// Modelled on <c>PlatformServices</c>: a registry filled by <c>[ModuleInitializer]</c> as each option's
/// assembly loads, frozen on first read, with the three phases as aggregate static entry points.
/// </para>
/// <para>
/// Static state on a generic base is per-closed-generic, so every collection gets its own registry and
/// its own funcs — the statics closed over <c>ConnectionTypeBase</c> are a different set from those
/// closed over any other domain's base. That is what lets one base serve every collection.
/// </para>
/// </remarks>
public abstract class ServiceTypeCollectionBase<TBase, TInterface>
    where TBase : class
    where TInterface : class, IServiceTypeRegistration
{
    private static readonly object _gate = new();

    // Pre-freeze staging. Written only by [ModuleInitializer] RegisterMember() calls, which the CLR
    // guarantees complete before Main() and thus before anything can trigger the freeze.
    private static readonly List<IServiceTypeRegistration> _pending = new();

    // Why an id set: membership is asked on every registration, from every assembly that loads, and
    // the answer must not change meaning when the collection closes. A set keyed on the option's id
    // answers in O(1) and is maintained at the one place membership changes.
    private static readonly HashSet<object> _registeredIds = new();

    private static IServiceTypeRegistration[] _frozenOptions = Array.Empty<IServiceTypeRegistration>();
    private static volatile bool _frozen;

    // Why TInterface names the collection: it is the one type name that identifies the collection
    // unambiguously at this altitude, and the freeze failure below already names it this way.
    private static string CollectionName => typeof(TInterface).Name;

    /// <summary>
    /// Adds an option to this collection. Called from the generated <c>[ModuleInitializer]</c> as the
    /// option's assembly loads — a package reference IS the registration.
    /// </summary>
    /// <param name="option">The option to register.</param>
    // Why fail loud after freeze: the collection is read to build providers, so an option registered
    // afterwards is invisible to everything already composed. NO FALLBACKS — a silent miss is exactly
    // the failure this registry exists to prevent.
    public static void RegisterMember(IServiceTypeRegistration option)
    {
        if (option is null)
            throw new ArgumentNullException(nameof(option));

        lock (_gate)
        {
            // Why idempotent: registration arrives from two directions — the collection's own
            // [ModuleInitializer] and, in an entry-point app, the cross-assembly
            // ServiceTypeOptionRegistration the Registration.SourceGenerators emits. Both name the
            // same option. First registration wins.
            //
            // Why membership is a set and why it is asked FIRST: re-offering a member that is already
            // present is a no-op at every point in the lifecycle, including after the set has closed —
            // the collection already holds it. Only a genuinely NEW member arriving after the close is
            // an error, because that one would never appear in any lookup. The set answers in O(1) and
            // means nothing here walks the frozen snapshot, which is a read-optimised copy rather than
            // a second registry to consult.
            if (!_registeredIds.Add(option.Id))
                return;

            if (_frozen)
            {
                // Why the id comes back out: this option is being rejected, so it is not a member.
                // Leaving it in the set would make a second attempt look like a duplicate and return
                // quietly — turning a loud, correct failure into a silent one.
                _registeredIds.Remove(option.Id);
                throw new InvalidOperationException(
                    $"The {typeof(TInterface).Name} collection was already read; registering '{option.Name}' now would be invisible to it.");
            }

            _pending.Add(option);
        }
    }

    /// <summary>Gets every registered option, freezing the collection on first call.</summary>
    protected static IServiceTypeRegistration[] Options
    {
        get
        {
            EnsureFrozen();
            return _frozenOptions;
        }
    }

    private static void EnsureFrozen()
    {
        if (_frozen)
            return;

        lock (_gate)
        {
            if (_frozen)
                return;

            _frozenOptions = _pending.ToArray();
            _frozen = true;
        }
    }

    // ── The three phases ────────────────────────────────────────────────────────────────────────
    // Each phase is a func holding the body, a gerund that replaces it, and a verb that invokes it.
    //
    // The defaults are set HERE, in the declaration, so a func is never null and the verb never has to
    // guard. They are static lambdas over static members, which a static field initializer may do —
    // an instance initializer could not, because a lambda touching Options would capture `this`.
    //
    // Each default sweeps this collection's options and runs that option's own phase.

    private static Func<IHostApplicationBuilder, ILoggerFactory?, IHostApplicationBuilder> _configurationFunc
        = static (builder, loggerFactory) =>
        {
            foreach (var option in Options)
                option.Configure(builder, loggerFactory);
            return builder;
        };

    private static Func<IHostApplicationBuilder, ILoggerFactory?, IHostApplicationBuilder> _registerFunc
        = static (builder, loggerFactory) =>
        {
            foreach (var option in Options)
            {
                option.Register(builder, loggerFactory,
                    option.DefaultDataStoreName, option.DefaultPathName, option.DefaultContainerName);
            }
            return builder;
        };

    private static Func<IServiceProvider, ILoggerFactory?, IServiceProvider> _initializationFunc
        = static (services, loggerFactory) =>
        {
            foreach (var option in Options)
                option.Initialize(services, loggerFactory);
            return services;
        };

    // ── Which body is installed ─────────────────────────────────────────────────────────────────
    // Set by the gerund setters, read by the invokers, so each phase can say at Info whether the
    // framework's body or an application's replacement is the one about to run.
    //
    // Why this is worth a log line: replacing a phase body is invisible from the outside. When a domain
    // silently fails to register, the first question is whether the option sweep everyone assumes runs
    // actually ran — and until now nothing in the process could answer it.
    //
    // These track APPLICATION replacement only. The generated part of a collection contributes its
    // provider registration as invariant wiring around the invoker, not by swapping the func, so
    // framework composition never reads as custom here.

    /// <summary>Gets a value indicating whether an application replaced this collection's Configure body.</summary>
    protected static bool ConfigurationIsCustom { get; private set; }

    /// <summary>Gets a value indicating whether an application replaced this collection's Register body.</summary>
    protected static bool RegistrationIsCustom { get; private set; }

    /// <summary>Gets a value indicating whether an application replaced this collection's Initialize body.</summary>
    protected static bool InitializationIsCustom { get; private set; }

    /// <summary>Gets this collection's Configure body.</summary>
    protected static Func<IHostApplicationBuilder, ILoggerFactory?, IHostApplicationBuilder> ConfigurationFunc => _configurationFunc;

    /// <summary>Gets this collection's Register body.</summary>
    protected static Func<IHostApplicationBuilder, ILoggerFactory?, IHostApplicationBuilder> RegisterFunc => _registerFunc;

    /// <summary>Gets this collection's Initialize body.</summary>
    protected static Func<IServiceProvider, ILoggerFactory?, IServiceProvider> InitializationFunc => _initializationFunc;

    /// <summary>Replaces this collection's Configure body. Call before phase 1.</summary>
    /// <param name="method">The replacement delegate.</param>
    public static void Configuration(Func<IHostApplicationBuilder, ILoggerFactory?, IHostApplicationBuilder> method)
    {
        _configurationFunc = method ?? throw new ArgumentNullException(nameof(method));
        ConfigurationIsCustom = true;
    }

    /// <summary>Replaces this collection's Register body. Call before phase 2.</summary>
    /// <param name="method">The replacement delegate.</param>
    public static void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IHostApplicationBuilder> method)
    {
        _registerFunc = method ?? throw new ArgumentNullException(nameof(method));
        RegistrationIsCustom = true;
    }

    /// <summary>Replaces this collection's Initialize body. Call before phase 3.</summary>
    /// <param name="method">The replacement delegate.</param>
    public static void Initialization(Func<IServiceProvider, ILoggerFactory?, IServiceProvider> method)
    {
        _initializationFunc = method ?? throw new ArgumentNullException(nameof(method));
        InitializationIsCustom = true;
    }

    /// <summary>Phase 1 — binds each option's configuration.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// The <c>xxxTypes</c> class this is called on — <c>ConnectionTypes</c>, <c>SecretManagerTypes</c>
    /// and the rest — is written by <c>ServiceTypeCollectionGenerator</c> from the
    /// <c>[ServiceTypeCollection]</c> attribute, not by hand. <c>ConnectionTypes.Configure(builder)</c>
    /// binds to this inherited static; the generated part supplies the registry contents and the typed
    /// lookups.
    /// </remarks>
    public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
        => RunPhase(builder, loggerFactory, "Configure", ConfigurationIsCustom,
            ServiceTypePhaseSequence.Configure, _configurationFunc);

    /// <summary>Phase 2 — each option registers its factory and configuration provider.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// The <c>xxxTypes</c> class this is called on is written by <c>ServiceTypeCollectionGenerator</c>
    /// from the <c>[ServiceTypeCollection]</c> attribute, not by hand. Where that attribute names a
    /// <c>ProviderType</c>, the generated part registers the provider into DI around this call, so the
    /// provider is wired whether or not an application replaced the option sweep.
    /// </remarks>
    public static IHostApplicationBuilder Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
        => RunPhase(builder, loggerFactory, "Register", RegistrationIsCustom,
            ServiceTypePhaseSequence.Register, _registerFunc);

    /// <summary>Phase 3 — post-Build initialization.</summary>
    /// <param name="services">The built service provider.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The service provider, for chaining.</returns>
    /// <remarks>
    /// The <c>xxxTypes</c> class this is called on is written by <c>ServiceTypeCollectionGenerator</c>
    /// from the <c>[ServiceTypeCollection]</c> attribute, not by hand.
    /// </remarks>
    public static IServiceProvider Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null)
        => RunPhase(services, loggerFactory, "Initialize", InitializationIsCustom,
            ServiceTypePhaseSequence.Initialize, _initializationFunc);

    // Why one runner rather than three copies of the same ceremony: the phases differ only in what
    // flows through them, and a logging contract that drifts between phases is worse than none.
    //
    // Why log-and-rethrow rather than the usual catch-log-return: these return the builder or the
    // provider, so there is no failure value to hand back. Swallowing would let a half-registered
    // domain reach a running application, which is the outcome NO FALLBACKS exists to prevent. The log
    // names the phase and which body was running; the throw stops the host.
    private static T RunPhase<T>(
        T subject,
        ILoggerFactory? loggerFactory,
        string phase,
        bool isCustom,
        ServiceTypePhaseSequence sequence,
        Func<T, ILoggerFactory?, T> body)
    {
        var logger = loggerFactory?.CreateLogger(typeof(TInterface).FullName ?? CollectionName)
            ?? (ILogger)NullLogger.Instance;
        var position = sequence.BeginCollection(CollectionName);

        if (isCustom)
            ServiceTypeLog.CollectionPhaseCustom(logger, CollectionName, phase, position, ServiceTypeLog.PhaseDocumentation);
        else
            ServiceTypeLog.CollectionPhaseDefault(logger, CollectionName, phase, position, ServiceTypeLog.PhaseDocumentation);

        try
        {
            var result = body(subject, loggerFactory);
            ServiceTypeLog.CollectionPhaseSucceeded(logger, CollectionName, phase, position, Options.Length);
            return result;
        }
        catch (Exception ex)
        {
            ServiceTypeLog.CollectionPhaseFailed(logger, ex, CollectionName, phase, position, isCustom ? "custom" : "default");
            throw;
        }
    }
}
