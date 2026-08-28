using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;

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

    private static readonly HashSet<object> _registeredIds = new();

    private static IServiceTypeRegistration[] _frozenOptions = Array.Empty<IServiceTypeRegistration>();
    private static volatile bool _frozen;

    private static string CollectionName => typeof(TInterface).Name;

    /// <summary>
    /// Adds an option to this collection. Called from the generated <c>[ModuleInitializer]</c> as the
    /// option's assembly loads — a package reference IS the registration.
    /// </summary>
    /// <param name="option">The option to register.</param>
    public static void RegisterMember(IServiceTypeRegistration option)
    {
        if (option is null)
            throw new ArgumentNullException(nameof(option));

        lock (_gate)
        {
            if (!_registeredIds.Add(option.Id))
                return;

            if (_frozen)
            {
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
    private static PhaseState _configure;
    private static PhaseState _register;
    private static PhaseState _initialize;

    /// <summary>Gets whether Configure has not run, is deferred, or has run.</summary>
    public static PhaseState ConfigureState => _configure;

    /// <summary>Gets whether Register has not run, is deferred, or has run.</summary>
    public static PhaseState RegisterState => _register;

    /// <summary>Gets whether Initialize has not run, is deferred, or has run.</summary>
    public static PhaseState InitializeState => _initialize;

    /// <summary>Gets a value indicating whether Configure has run.</summary>
    public static bool Configured => _configure == PhaseState.Ran;

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public static bool Registered => _register == PhaseState.Ran;

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public static bool Initialized => _initialize == PhaseState.Ran;

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
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (!force && _configure == PhaseState.Ran)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        if (defer)
        {
            _configure = PhaseState.Deferred;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var phaseResult = RunPhase(builder, loggerFactory, "Configure", ConfigurationIsCustom,
            ServiceTypePhaseSequence.Configure, _configurationFunc);
        if (phaseResult.IsFailure)
        {
            return phaseResult;
        }

        _configure = PhaseState.Ran;
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
    /// provider is wired whether or not an application replaced the option collect.
    /// </remarks>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
        /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (!force && _register == PhaseState.Ran)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        if (defer)
        {
            _register = PhaseState.Deferred;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var phaseResult = RunPhase(builder, loggerFactory, "Register", RegistrationIsCustom,
            ServiceTypePhaseSequence.Register, _registerFunc);
        if (phaseResult.IsFailure)
        {
            return phaseResult;
        }

        _register = PhaseState.Ran;
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
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
        /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (!force && _initialize == PhaseState.Ran)
        {
            return GenericResult<IHost>.Success(host);
        }

        if (defer)
        {
            _initialize = PhaseState.Deferred;
            return GenericResult<IHost>.Success(host);
        }

        var phaseResult = RunPhase(host, loggerFactory, "Initialize", InitializationIsCustom,
            ServiceTypePhaseSequence.Initialize, _initializationFunc);
        if (phaseResult.IsFailure)
        {
            return phaseResult;
        }

        _initialize = PhaseState.Ran;
        return phaseResult;
    }

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

            if (result.IsSuccess)
            {
                if (Options.Length == 0)
                    ServiceTypeLog.CollectionPhaseNoOptions(logger, CollectionName, phase, position);
                else
                    ServiceTypeLog.CollectionPhaseSucceeded(logger, CollectionName, phase, position, Options.Length);
            }

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
