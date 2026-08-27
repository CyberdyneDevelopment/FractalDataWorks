using System;
using System.Runtime.CompilerServices;
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

    private Guid? _id;

    /// <inheritdoc />
    // Why the fully-qualified type name and not the option's name: Id is global across every
    // collection, and a bare name is not. Fifteen collections each declare an option named "Default",
    // and MD5 over the name alone hands all fifteen the same Guid; "MsSql", "Sqlite", "Http", "Sql" and
    // "OpenIddict" collide across two apiece. A type's FQN is unique by construction, so the id is too.
    public override Guid Id => _id ??= OptionId.Derive(GetType().FullName ?? Name);
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

    private PhaseState _configure;
    private PhaseState _register;
    private PhaseState _initialize;

    /// <summary>Gets whether Configure has not run, is deferred, or has run.</summary>
    /// <remarks>
    /// A phase runs once. Idempotence is what makes chaining safe: a body appended by one contributor
    /// cannot re-run what an earlier one already did, however many times a phase is invoked.
    /// </remarks>
    public PhaseState ConfigureState => _configure;

    /// <summary>Gets whether Register has not run, is deferred, or has run.</summary>
    public PhaseState RegisterState => _register;

    /// <summary>Gets whether Initialize has not run, is deferred, or has run.</summary>
    public PhaseState InitializeState => _initialize;

    /// <summary>Gets a value indicating whether Configure has run.</summary>
    public bool Configured => _configure == PhaseState.Ran;

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public bool Registered => _register == PhaseState.Ran;

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public bool Initialized => _initialize == PhaseState.Ran;


    /// <summary>
    /// Sets the body this service type runs during Configure — the phase that binds settings from configuration, before any service is registered.
    /// </summary>
    /// <remarks>
    /// This is the call a <c>[ServiceTypeOption]</c> or <c>[ServiceTypeCollection]</c> class makes, and
    /// the only one it should need. A phase holds one body and the class declaring the phase owns that
    /// body outright, so setting it states what this service type contributes rather than overwriting
    /// somebody's work. Reach for <see cref="AppendConfiguration"/> or <see cref="PrependConfiguration"/> only from
    /// OUTSIDE the declaring class, to customise a service type this code did not author — used from
    /// inside it, they are STC001.
    /// </remarks>
    /// <param name="method">The body this phase runs.</param>
    public void Configuration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Configure", nameof(method));
            return;
        }

        ConfigurationMethod = method;
    }

    /// <summary>
    /// Runs <paramref name="method"/> after the body already installed, for adding to a service type
    /// declared somewhere else.
    /// </summary>
    /// <remarks>
    /// Use this from OUTSIDE the declaring class — a host or package extending a service type it did not
    /// author, which cannot edit that type's own body and must not silently drop it. Inside the class that
    /// declares the phase it is an error (STC001): the class owns its phase, so it says what it contributes
    /// with <see cref="Configuration"/>, and where it genuinely needs another func to run too, it captures that
    /// func in a local and calls it at the point in the body where it belongs — visible, ordered, and
    /// debuggable, rather than nested behind a call that reads as though nothing else is there.
    /// </remarks>
    /// <param name="method">The body to run after the one already installed.</param>
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

    /// <summary>
    /// Runs <paramref name="method"/> before the body already installed, for adding to a service type
    /// declared somewhere else.
    /// </summary>
    /// <remarks>
    /// The ordering counterpart to <see cref="AppendConfiguration"/>, and governed by the same rule: use it from
    /// OUTSIDE the declaring class, where the added wiring has to be in place before that type's own body
    /// runs. Inside the class that declares the phase it is an error (STC001), and from a base class in the
    /// middle of the chain it is STC002 — a base prepending here leaves every derived type unable to set its
    /// own body without silently discarding this one. Wiring that every option of a domain needs belongs in
    /// the collection's Register, where the option set is already in hand.
    /// </remarks>
    /// <param name="method">The body to run before the one already installed.</param>
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

    /// <summary>
    /// Sets the body this service type runs during Register — the phase that puts services into the container, before the host is built.
    /// </summary>
    /// <remarks>
    /// This is the call a <c>[ServiceTypeOption]</c> or <c>[ServiceTypeCollection]</c> class makes, and
    /// the only one it should need. A phase holds one body and the class declaring the phase owns that
    /// body outright, so setting it states what this service type contributes rather than overwriting
    /// somebody's work. Reach for <see cref="AppendRegistration"/> or <see cref="PrependRegistration"/> only from
    /// OUTSIDE the declaring class, to customise a service type this code did not author — used from
    /// inside it, they are STC001.
    /// </remarks>
    /// <param name="method">The body this phase runs.</param>
    /// <param name="filePath">Compiler-supplied path of the file this call is written in; do not pass it.</param>
    /// <param name="lineNumber">Compiler-supplied line this call is written on; do not pass it.</param>
    /// <param name="memberName">Compiler-supplied name of the member this call is written in; do not pass it.</param>
    public void Registration(
        Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Register", nameof(method));
            return;
        }

        // Why setting resets the count: this is the service type stating what it contributes, so whatever
        // was installed before is gone and the body being set is the phase's first and only segment.
        _registrationSegments = 1;
        var origin = Origin(filePath, lineNumber, memberName);
        RegistrationMethod = (builder, loggerFactory) =>
            RunRegistrationSegment(builder, loggerFactory, method, origin);
    }

    /// <summary>
    /// Runs <paramref name="method"/> after the body already installed, for adding to a service type
    /// declared somewhere else.
    /// </summary>
    /// <remarks>
    /// Use this from OUTSIDE the declaring class — a host or package extending a service type it did not
    /// author, which cannot edit that type's own body and must not silently drop it. Inside the class that
    /// declares the phase it is an error (STC001): the class owns its phase, so it says what it contributes
    /// with <see cref="Registration"/>, and where it genuinely needs another func to run too, it captures that
    /// func in a local and calls it at the point in the body where it belongs — visible, ordered, and
    /// debuggable, rather than nested behind a call that reads as though nothing else is there.
    /// </remarks>
    /// <param name="method">The body to run after the one already installed.</param>
    /// <param name="filePath">Compiler-supplied path of the file this call is written in; do not pass it.</param>
    /// <param name="lineNumber">Compiler-supplied line this call is written on; do not pass it.</param>
    /// <param name="memberName">Compiler-supplied name of the member this call is written in; do not pass it.</param>
    public void AppendRegistration(
        Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Register", nameof(method));
            return;
        }

        _registrationSegments++;
        var origin = Origin(filePath, lineNumber, memberName);
        var existing = RegistrationMethod;
        RegistrationMethod = (builder, loggerFactory) =>
        {
            var result = existing(builder, loggerFactory);
            return result.IsFailure ? result : RunRegistrationSegment(builder, loggerFactory, method, origin);
        };
    }

    /// <summary>
    /// Runs <paramref name="method"/> before the body already installed, for adding to a service type
    /// declared somewhere else.
    /// </summary>
    /// <remarks>
    /// The ordering counterpart to <see cref="AppendRegistration"/>, and governed by the same rule: use it from
    /// OUTSIDE the declaring class, where the added wiring has to be in place before that type's own body
    /// runs. Inside the class that declares the phase it is an error (STC001), and from a base class in the
    /// middle of the chain it is STC002 — a base prepending here leaves every derived type unable to set its
    /// own body without silently discarding this one. Wiring that every option of a domain needs belongs in
    /// the collection's Register, where the option set is already in hand.
    /// </remarks>
    /// <param name="method">The body to run before the one already installed.</param>
    /// <param name="filePath">Compiler-supplied path of the file this call is written in; do not pass it.</param>
    /// <param name="lineNumber">Compiler-supplied line this call is written on; do not pass it.</param>
    /// <param name="memberName">Compiler-supplied name of the member this call is written in; do not pass it.</param>
    public void PrependRegistration(
        Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Register", nameof(method));
            return;
        }

        _registrationSegments++;
        var origin = Origin(filePath, lineNumber, memberName);
        var existing = RegistrationMethod;
        RegistrationMethod = (builder, loggerFactory) =>
        {
            var result = RunRegistrationSegment(builder, loggerFactory, method, origin);
            return result.IsFailure ? result : existing(builder, loggerFactory);
        };
    }

    /// <summary>
    /// Sets the body this service type runs during Initialize — the phase that runs against the built host, where the container can be resolved from.
    /// </summary>
    /// <remarks>
    /// This is the call a <c>[ServiceTypeOption]</c> or <c>[ServiceTypeCollection]</c> class makes, and
    /// the only one it should need. A phase holds one body and the class declaring the phase owns that
    /// body outright, so setting it states what this service type contributes rather than overwriting
    /// somebody's work. Reach for <see cref="AppendInitialization"/> or <see cref="PrependInitialization"/> only from
    /// OUTSIDE the declaring class, to customise a service type this code did not author — used from
    /// inside it, they are STC001.
    /// </remarks>
    /// <param name="method">The body this phase runs.</param>
    /// <param name="filePath">Compiler-supplied path of the file this call is written in; do not pass it.</param>
    /// <param name="lineNumber">Compiler-supplied line this call is written on; do not pass it.</param>
    /// <param name="memberName">Compiler-supplied name of the member this call is written in; do not pass it.</param>
    public void Initialization(
        Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Initialize", nameof(method));
            return;
        }

        // Why setting resets the count: this is the service type stating what it contributes, so whatever
        // was installed before is gone and the body being set is the phase's first and only segment.
        _initializationSegments = 1;
        var origin = Origin(filePath, lineNumber, memberName);
        InitializationMethod = (host, loggerFactory) =>
            RunInitializationSegment(host, loggerFactory, method, origin);
    }

    /// <summary>
    /// Runs <paramref name="method"/> after the body already installed, for adding to a service type
    /// declared somewhere else.
    /// </summary>
    /// <remarks>
    /// Use this from OUTSIDE the declaring class — a host or package extending a service type it did not
    /// author, which cannot edit that type's own body and must not silently drop it. Inside the class that
    /// declares the phase it is an error (STC001): the class owns its phase, so it says what it contributes
    /// with <see cref="Initialization"/>, and where it genuinely needs another func to run too, it captures that
    /// func in a local and calls it at the point in the body where it belongs — visible, ordered, and
    /// debuggable, rather than nested behind a call that reads as though nothing else is there.
    /// </remarks>
    /// <param name="method">The body to run after the one already installed.</param>
    /// <param name="filePath">Compiler-supplied path of the file this call is written in; do not pass it.</param>
    /// <param name="lineNumber">Compiler-supplied line this call is written on; do not pass it.</param>
    /// <param name="memberName">Compiler-supplied name of the member this call is written in; do not pass it.</param>
    public void AppendInitialization(
        Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Initialize", nameof(method));
            return;
        }

        _initializationSegments++;
        var origin = Origin(filePath, lineNumber, memberName);
        var existing = InitializationMethod;
        InitializationMethod = (host, loggerFactory) =>
        {
            var result = existing(host, loggerFactory);
            return result.IsFailure ? result : RunInitializationSegment(host, loggerFactory, method, origin);
        };
    }

    /// <summary>
    /// Runs <paramref name="method"/> before the body already installed, for adding to a service type
    /// declared somewhere else.
    /// </summary>
    /// <remarks>
    /// The ordering counterpart to <see cref="AppendInitialization"/>, and governed by the same rule: use it from
    /// OUTSIDE the declaring class, where the added wiring has to be in place before that type's own body
    /// runs. Inside the class that declares the phase it is an error (STC001), and from a base class in the
    /// middle of the chain it is STC002 — a base prepending here leaves every derived type unable to set its
    /// own body without silently discarding this one. Wiring that every option of a domain needs belongs in
    /// the collection's Register, where the option set is already in hand.
    /// </remarks>
    /// <param name="method">The body to run before the one already installed.</param>
    /// <param name="filePath">Compiler-supplied path of the file this call is written in; do not pass it.</param>
    /// <param name="lineNumber">Compiler-supplied line this call is written on; do not pass it.</param>
    /// <param name="memberName">Compiler-supplied name of the member this call is written in; do not pass it.</param>
    public void PrependInitialization(
        Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        if (method is null)
        {
            ServiceTypeLog.PhaseBodyNull(NullLogger.Instance, Name, "Initialize", nameof(method));
            return;
        }

        _initializationSegments++;
        var origin = Origin(filePath, lineNumber, memberName);
        var existing = InitializationMethod;
        InitializationMethod = (host, loggerFactory) =>
        {
            var result = RunInitializationSegment(host, loggerFactory, method, origin);
            return result.IsFailure ? result : existing(host, loggerFactory);
        };
    }


    // ── Segment bookkeeping ─────────────────────────────────────────────────────────────────────
    // Why a phase counts its segments: a phase assembled from more than one contributor reports "Register
    // failed" without saying whose body failed, and the funcs are closures, so a stack trace names the
    // lambda and not the place it was written. The count and the run position give the failure a position
    // in the order, and the origin captured at the call site gives it the file and line of the Append or
    // Prepend that put it there — the one point that still knows.

    private int _registrationSegments = 1;
    private int _initializationSegments = 1;
    private int _registrationRunPosition;
    private int _initializationRunPosition;

    /// <summary>Names the call site that installed a phase segment.</summary>
    private static string Origin(string filePath, int lineNumber, string memberName) =>
        $"{memberName} at {System.IO.Path.GetFileName(filePath)}:{lineNumber}";

    private IGenericResult<IHostApplicationBuilder> RunRegistrationSegment(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory,
        Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> segment,
        string origin)
    {
        var logger = loggerFactory?.CreateLogger(GetType().FullName ?? Name) ?? (ILogger)NullLogger.Instance;
        var position = ++_registrationRunPosition;

        ServiceTypeLog.PhaseSegmentRunning(logger, Name, "Register", position, _registrationSegments, origin);
        var result = segment(builder, loggerFactory);

        if (result.IsSuccess)
            ServiceTypeLog.PhaseSegmentSucceeded(logger, Name, "Register", position, _registrationSegments, origin);

        return result;
    }

    private IGenericResult<IHost> RunInitializationSegment(
        IHost host,
        ILoggerFactory? loggerFactory,
        Func<IHost, ILoggerFactory?, IGenericResult<IHost>> segment,
        string origin)
    {
        var logger = loggerFactory?.CreateLogger(GetType().FullName ?? Name) ?? (ILogger)NullLogger.Instance;
        var position = ++_initializationRunPosition;

        ServiceTypeLog.PhaseSegmentRunning(logger, Name, "Initialize", position, _initializationSegments, origin);
        var result = segment(host, loggerFactory);

        if (result.IsSuccess)
            ServiceTypeLog.PhaseSegmentSucceeded(logger, Name, "Initialize", position, _initializationSegments, origin);

        return result;
    }

    // ── The bodies themselves ───────────────────────────────────────────────────────────────────
    // Why they sit below the setters: the setters are the surface a service type writes against, and
    // these are where what it wrote ends up. Reading the file in that order matches the order the
    // question is usually asked in - what do I call, and then what does it hold.
    //
    // Each defaults to a body that does nothing but succeed, so a service type that has nothing to say
    // in a phase says nothing, and the phase still reports that it ran. An option that never set a body
    // is otherwise indistinguishable, from outside, from one whose body ran and did nothing - which is
    // the first fact worth having when a service fails to resolve later, and the hardest to recover
    // after the fact.

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

    // Why none of these is virtual: an override is invisible to the chain, which invokes the func a
    // level holds. The reason one used to be needed — a base contributing wiring that a derived
    // Registration(...) would clobber — is what Append and Prepend remove.

    /// <inheritdoc />
    /// <remarks>
    /// The collection that collects this option — <c>ConnectionTypes</c> and the rest — is written by
    /// <c>ServiceTypeCollectionGenerator</c> from the <c>[ServiceTypeCollection]</c> attribute, not by
    /// hand. It is that generated collect which calls this, in the order the log line reports.
    /// </remarks>
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (!force && _configure == PhaseState.Ran)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        // Why the claim happens before the work and not after: a deferred phase must look done to the
        // collect without having run, which is the one thing a bool latch cannot express.
        if (defer)
        {
            _configure = PhaseState.Deferred;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = RunPhase(loggerFactory, "Configure", ServiceTypePhaseSequence.Configure,
            () => ConfigurationMethod(builder));
        // Why the latch is only set on success: this flag is what makes the phase run-once, and the
        // early return above turns an already-latched phase into an unconditional Success. Setting it
        // after a failure therefore records a phase that did not happen as done, and every later call
        // reports success for work that never ran - the failure is logged once and then permanently
        // papered over. Returning first leaves the phase un-latched so a caller that retries actually
        // retries.
        if (result.IsFailure)
        {
            return result;
        }

        _configure = PhaseState.Ran;
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Called by the generated collection's phase-2 collect. Where the collection declares a
    /// <c>ProviderType</c>, the generated part registers that provider independently of this call.
    /// </remarks>
    public IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        bool force = false,
        bool defer = false)
    {
        if (!force && _register == PhaseState.Ran)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        // Why the claim happens before the work and not after: a deferred phase must look done to the
        // collect without having run, which is the one thing a bool latch cannot express.
        if (defer)
        {
            _register = PhaseState.Deferred;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        _registrationRunPosition = 0;
        var result = RunPhase(loggerFactory, "Register", ServiceTypePhaseSequence.Register,
            () => RegistrationMethod(builder, loggerFactory));
        // Why the latch is only set on success: this flag is what makes the phase run-once, and the
        // early return above turns an already-latched phase into an unconditional Success. Setting it
        // after a failure therefore records a phase that did not happen as done, and every later call
        // reports success for work that never ran - the failure is logged once and then permanently
        // papered over. Returning first leaves the phase un-latched so a caller that retries actually
        // retries.
        if (result.IsFailure)
        {
            return result;
        }

        _register = PhaseState.Ran;
        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Called by the generated collection's phase-3 collect, after the host has been built.
    /// </remarks>
    public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        if (!force && _initialize == PhaseState.Ran)
        {
            return GenericResult<IHost>.Success(host);
        }

        // Why the claim happens before the work and not after: a deferred phase must look done to the
        // collect without having run, which is the one thing a bool latch cannot express.
        if (defer)
        {
            _initialize = PhaseState.Deferred;
            return GenericResult<IHost>.Success(host);
        }

        _initializationRunPosition = 0;
        var result = RunPhase(loggerFactory, "Initialize", ServiceTypePhaseSequence.Initialize,
            () => InitializationMethod(host, loggerFactory));
        // Why the latch is only set on success: this flag is what makes the phase run-once, and the
        // early return above turns an already-latched phase into an unconditional Success. Setting it
        // after a failure therefore records a phase that did not happen as done, and every later call
        // reports success for work that never ran - the failure is logged once and then permanently
        // papered over. Returning first leaves the phase un-latched so a caller that retries actually
        // retries.
        if (result.IsFailure)
        {
            return result;
        }

        _initialize = PhaseState.Ran;
        return result;
    }

    // Why the body arrives as a thunk rather than the func itself: the three phases take different
    // arguments, and closing over them here keeps one logging contract instead of three that drift.
    //
    // Why catch-log-return rather than log-and-rethrow: an exception decides for the application that
    // the process ends. A framework does not get to make that call — the host may want to abort on a
    // failed domain or run without it, and it can only choose if the failure arrives as a value. The
    // catch is the boundary where an option that still throws is converted into the result everything
    // above this expects, so one badly-behaved option cannot unwind a collect that was handling failures.
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
