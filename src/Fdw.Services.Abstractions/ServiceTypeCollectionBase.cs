using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes.Results;
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

    // ── Configure, Register, Initialize ─────────────────────────────────────────────────────────
    // Each is a func holding the body, a gerund that replaces it, and a verb that invokes it.
    //
    // The defaults are set HERE, in the declaration, so a func is never null and the verb never has to
    // guard. They are static lambdas over static members, which a static field initializer may do —
    // an instance initializer could not, because a lambda touching Options would capture `this`.
    //
    // Each default calls the same method on every option in this collection.

    // Why a default stops at the first failing option instead of running the rest: the options after it
    // register against a domain that is already incomplete, and whatever they build on top of the
    // missing piece fails later, somewhere else, with nothing pointing back here. Stopping means the
    // failure the caller receives is the FIRST one, which is the one that explains the others.
    // NO FALLBACKS — it does not carry on and hope.

    private static Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> _configurationFunc
        = static (builder, loggerFactory) =>
        {
            foreach (var option in Options)
            {
                var result = option.Configure(builder, loggerFactory);
                if (result.IsFailure)
                    return Stop<IHostApplicationBuilder>(loggerFactory, "Configure", option, result);
            }

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        };

    private static Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> _registerFunc
        = static (builder, loggerFactory) =>
        {
            foreach (var option in Options)
            {
                // Why not threaded in: the option already exposes DefaultDataStoreName, DefaultPathName
                // and DefaultContainerName, so passing them back to it was the collection reading three
                // values off an option and handing them straight back — on every call site, for the few
                // bodies that read them.
                var result = option.Register(builder, loggerFactory);
                if (result.IsFailure)
                    return Stop<IHostApplicationBuilder>(loggerFactory, "Register", option, result);
            }

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        };

    private static Func<IHost, ILoggerFactory?, IGenericResult<IHost>> _initializationFunc
        = static (host, loggerFactory) =>
        {
            foreach (var option in Options)
            {
                var result = option.Initialize(host, loggerFactory);
                if (result.IsFailure)
                    return Stop<IHost>(loggerFactory, "Initialize", option, result);
            }

            return GenericResult<IHost>.Success(host);
        };

    // Why the option's own failure is carried forward rather than a fresh one: it already names what
    // went wrong in that option's own vocabulary. Re-wrapping would bury the specific code under a
    // generic one. The log line is what adds the context — which collection, which method, which option.
    //
    // Why ToNewResult and not Failure(failure.Messages): copying the messages out and building a new
    // result drops the error CHAIN, so the caller sees the leaf complaint with nothing linking it to
    // what it came from (FDW015 reports exactly this). ToNewResult re-types the same result, keeping
    // the chain intact across the generic boundary.
    private static IGenericResult<T> Stop<T>(
        ILoggerFactory? loggerFactory,
        string phase,
        IServiceTypeRegistration option,
        IGenericResult failure)
    {
        ServiceTypeLog.CollectionPhaseStopped(
            loggerFactory?.CreateLogger(typeof(TInterface).FullName ?? CollectionName) ?? (ILogger)NullLogger.Instance,
            CollectionName, phase, option.Name, failure.CurrentMessage ?? string.Empty);

        return failure.ToNewResult<T>();
    }

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
    /// <summary>Gets a value indicating whether Configure has run.</summary>
    /// <remarks>
    /// Per closed generic, like everything else static here: each collection tracks its own phases,
    /// and a phase runs once so that a chained body cannot re-cycle members an earlier one already did.
    /// </remarks>
    public static bool Configured { get; private set; }

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public static bool Registered { get; private set; }

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public static bool Initialized { get; private set; }

    /// <summary>Gets or sets a value indicating whether Configure is switched off.</summary>
    /// <remarks>
    /// One flag per phase, because they are switched off for different reasons: a domain may need
    /// its services registered while its post-Build wiring is suppressed.
    /// </remarks>
    public static bool SkipConfiguration { get; set; }

    /// <summary>Gets or sets a value indicating whether Register is switched off.</summary>
    public static bool SkipRegistration { get; set; }

    /// <summary>Gets or sets a value indicating whether Initialize is switched off.</summary>
    public static bool SkipInitialization { get; set; }

    /// <summary>Gets this collection's Configure body.</summary>
    protected static Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> ConfigurationFunc => _configurationFunc;

    /// <summary>Gets this collection's Register body.</summary>
    protected static Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> RegisterFunc => _registerFunc;

    /// <summary>Gets this collection's Initialize body.</summary>
    protected static Func<IHost, ILoggerFactory?, IGenericResult<IHost>> InitializationFunc => _initializationFunc;


    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>
    /// Prefer this to <see cref="Configuration"/>, which assigns and so discards the member cycle this
    /// collection starts with, along with anything another contributor added.
    /// </remarks>
    /// <param name="method">The body to run after.</param>
    public static void AppendConfiguration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = _configurationFunc;
        _configurationFunc = (builder, loggerFactory) =>
        {
            var result = existing(builder, loggerFactory);
            return result.IsFailure ? result : method(builder, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public static void PrependConfiguration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = _configurationFunc;
        _configurationFunc = (builder, loggerFactory) =>
        {
            var result = method(builder, loggerFactory);
            return result.IsFailure ? result : existing(builder, loggerFactory);
        };
    }

    /// <summary>Replaces this collection's Configure body. Call before Configure runs.</summary>
    /// <param name="method">The replacement delegate.</param>
    public static void Configuration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        _configurationFunc = method ?? throw new ArgumentNullException(nameof(method));
        ConfigurationIsCustom = true;
    }


    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>
    /// Prefer this to <see cref="Registration"/>, which assigns and so discards the member cycle this
    /// collection starts with, along with anything another contributor added.
    /// </remarks>
    /// <param name="method">The body to run after.</param>
    public static void AppendRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = _registerFunc;
        _registerFunc = (builder, loggerFactory) =>
        {
            var result = existing(builder, loggerFactory);
            return result.IsFailure ? result : method(builder, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public static void PrependRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = _registerFunc;
        _registerFunc = (builder, loggerFactory) =>
        {
            var result = method(builder, loggerFactory);
            return result.IsFailure ? result : existing(builder, loggerFactory);
        };
    }

    /// <summary>Replaces this collection's Register body. Call before Register runs.</summary>
    /// <param name="method">The replacement delegate.</param>
    public static void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        _registerFunc = method ?? throw new ArgumentNullException(nameof(method));
        RegistrationIsCustom = true;
    }


    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>
    /// Prefer this to <see cref="Initialization"/>, which assigns and so discards the member cycle this
    /// collection starts with, along with anything another contributor added.
    /// </remarks>
    /// <param name="method">The body to run after.</param>
    public static void AppendInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = _initializationFunc;
        _initializationFunc = (host, loggerFactory) =>
        {
            var result = existing(host, loggerFactory);
            return result.IsFailure ? result : method(host, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public static void PrependInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = _initializationFunc;
        _initializationFunc = (host, loggerFactory) =>
        {
            var result = method(host, loggerFactory);
            return result.IsFailure ? result : existing(host, loggerFactory);
        };
    }

    /// <summary>Replaces this collection's Initialize body. Call before Initialize runs.</summary>
    /// <param name="method">The replacement delegate.</param>
    public static void Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        _initializationFunc = method ?? throw new ArgumentNullException(nameof(method));
        InitializationIsCustom = true;
    }

    /// <summary>Binds each option's configuration.</summary>
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
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        if (Configured || SkipConfiguration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var phaseResult = RunPhase(builder, loggerFactory, "Configure", ConfigurationIsCustom,
            ServiceTypePhaseSequence.Configure, _configurationFunc);
        Configured = true;
        return phaseResult;
    }

    /// <summary>Each option registers its factory and configuration provider.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// The <c>xxxTypes</c> class this is called on is written by <c>ServiceTypeCollectionGenerator</c>
    /// from the <c>[ServiceTypeCollection]</c> attribute, not by hand. Where that attribute names a
    /// <c>ProviderType</c>, the generated part registers the provider into DI around this call, so the
    /// provider is wired whether or not an application replaced the option sweep.
    /// </remarks>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        if (Registered || SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var phaseResult = RunPhase(builder, loggerFactory, "Register", RegistrationIsCustom,
            ServiceTypePhaseSequence.Register, _registerFunc);
        Registered = true;
        return phaseResult;
    }

    /// <summary>Post-Build initialization.</summary>
    /// <param name="host">The built host. Its <c>Services</c> is the provider this phase used to take.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The host, for chaining.</returns>
    /// <remarks>
    /// The <c>xxxTypes</c> class this is called on is written by <c>ServiceTypeCollectionGenerator</c>
    /// from the <c>[ServiceTypeCollection]</c> attribute, not by hand.
    /// </remarks>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
    {
        if (Initialized || SkipInitialization)
        {
            return GenericResult<IHost>.Success(host);
        }

        var phaseResult = RunPhase(host, loggerFactory, "Initialize", InitializationIsCustom,
            ServiceTypePhaseSequence.Initialize, _initializationFunc);
        Initialized = true;
        return phaseResult;
    }

    // Why one runner rather than three copies of the same ceremony: the phases differ only in what
    // flows through them, and a logging contract that drifts between phases is worse than none.
    //
    // Why catch-log-return rather than log-and-rethrow: a throw ends the process, which is a decision
    // about THIS application that this framework type is in no position to make. Returning the failure
    // hands that decision to whoever composed the host — abort, or run without this domain — while
    // still guaranteeing they cannot proceed unaware, because they must read the result to get the
    // builder back out of it. NO FALLBACKS is satisfied by refusing to return a success, not by
    // choosing the caller's error handling for them.
    //
    // The catch stays because a phase body is arbitrary code that may still throw; this is the seam
    // that turns that into the value the rest of the pipeline is written against.
    private static IGenericResult<T> RunPhase<T>(
        T subject,
        ILoggerFactory? loggerFactory,
        string phase,
        bool isCustom,
        ServiceTypePhaseSequence sequence,
        Func<T, ILoggerFactory?, IGenericResult<T>> body)
    {
        var logger = loggerFactory?.CreateLogger(typeof(TInterface).FullName ?? CollectionName)
            ?? (ILogger)NullLogger.Instance;
        var position = sequence.BeginCollection(CollectionName);
        var implementation = isCustom ? "custom" : "default";

        if (isCustom)
            ServiceTypeLog.CollectionPhaseCustom(logger, CollectionName, phase, position, ServiceTypeLog.PhaseDocumentation);
        else
            ServiceTypeLog.CollectionPhaseDefault(logger, CollectionName, phase, position, ServiceTypeLog.PhaseDocumentation);

        try
        {
            var result = body(subject, loggerFactory);

            // Why only the success line is conditional: a failure has already been logged with the
            // option that caused it by the sweep, or by the option's own runner. Logging it again here
            // would report one failure twice, at two altitudes, as if they were two events.
            if (result.IsSuccess)
                ServiceTypeLog.CollectionPhaseSucceeded(logger, CollectionName, phase, position, Options.Length);

            return result;
        }
        catch (Exception ex)
        {
            ServiceTypeLog.CollectionPhaseFailed(logger, ex, CollectionName, phase, position, implementation);
            return GenericResult<T>.Failure(
                ServiceTypeResultCodes.ByName("CollectionPhaseFailed"),
                ResultDetails.Create("CollectionName", CollectionName)
                    .With("Phase", phase)
                    .With("Sequence", position)
                    .With("Implementation", implementation)
                    .With("ErrorMessage", ex.Message));
        }
    }
}
