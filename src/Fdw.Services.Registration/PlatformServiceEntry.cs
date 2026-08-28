using System;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Services.Abstractions;

namespace Fdw.ServiceTypes;

/// <summary>
/// One registered ServiceTypeCollection domain and its per-phase run state.
/// </summary>
/// <param name="CategoryName">Category name (e.g. "Connection") — matches the collection's <c>ServiceCategory</c>.</param>
/// <param name="Descriptor">Descriptor exposing the collection's Configure/Register/Initialize entry points.</param>
/// <remarks>
/// The per-phase state is mutated in place rather than replaced: the entry is a reference type, so a
/// phase advancing on one shared instance is visible everywhere that instance is held, with no array
/// rebuild. Order is not a property of the entry — a host controls it by running a domain ahead of the
/// collect, or deferring it out of the collect and running it afterwards.
/// </remarks>
public sealed record PlatformServiceEntry(string CategoryName, IServiceTypeCollection Descriptor)
{
    /// <summary>Whether <see cref="Configure"/> has already run for this entry.</summary>
    private PhaseState _configure;

    /// <summary>Gets whether this phase has not run, is deferred, or has run.</summary>
    public PhaseState ConfigureState => _configure;

    /// <summary>Gets a value indicating whether the phase has run.</summary>
    public bool Configured => _configure == PhaseState.Ran;

    /// <summary>Whether <see cref="Initialize"/> has already run for this entry.</summary>
    private PhaseState _initialize;

    /// <summary>Gets whether this phase has not run, is deferred, or has run.</summary>
    public PhaseState InitializeState => _initialize;

    /// <summary>Gets a value indicating whether the phase has run.</summary>
    public bool Initialized => _initialize == PhaseState.Ran;

    /// <summary>Whether <see cref="Register"/> has already run for this entry.</summary>
    private PhaseState _register;

    /// <summary>Gets whether this phase has not run, is deferred, or has run.</summary>
    public PhaseState RegisterState => _register;

    /// <summary>Gets a value indicating whether the phase has run.</summary>
    public bool Registered => _register == PhaseState.Ran;

    /// <summary>
    /// Whether this domain is excluded from the <see cref="PlatformServices"/> collects
    /// (<see cref="PlatformServices.Configure"/>/<see cref="PlatformServices.Register"/>/
    // ── Phase-delegate replacements (author-curated variant selection; the keyset stays frozen) ─────────
    private Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>>? _configurationMethod;
    private Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>>? _registrationMethod;
    private Func<IHost, ILoggerFactory?, IGenericResult<IHost>>? _initializationMethod;

    /// <summary>
    /// Selects an alternative Configure phase delegate for this entry, replacing the descriptor default.
    /// Intended for a collection author to expose named variants (e.g. <c>UseThinClient()</c> that forwards
    /// an author-written delegate here); the raw setter is the documented escape hatch. Must be called
    /// BEFORE the Configure collect runs for this entry. Returns <c>this</c> for fluent chaining.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Configure phase has already run for this entry.</exception>
    public PlatformServiceEntry Configuration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (Configured)
            throw new InvalidOperationException(
                $"Cannot replace Configure for '{CategoryName}': the Configure phase has already run. " +
                "Phase replacements must be selected before the collect (lock-at-collect).");
        _configurationMethod = replacement;
        return this;
    }

    /// <summary>
    /// Selects an alternative Register phase delegate for this entry, replacing the descriptor default.
    /// See <see cref="Configuration"/> for intended (author-variant) vs escape-hatch usage. Must be
    /// called BEFORE the Register collect runs for this entry. Returns <c>this</c> for fluent chaining.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Register phase has already run for this entry.</exception>
    public PlatformServiceEntry Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (Registered)
            throw new InvalidOperationException(
                $"Cannot replace Register for '{CategoryName}': the Register phase has already run. " +
                "Phase replacements must be selected before the collect (lock-at-collect).");
        _registrationMethod = replacement;
        return this;
    }

    /// <summary>
    /// Selects an alternative Initialize phase delegate for this entry, replacing the descriptor default.
    /// See <see cref="Configuration"/> for intended (author-variant) vs escape-hatch usage. Must be
    /// called BEFORE the Initialize collect runs for this entry. Returns <c>this</c> for fluent chaining.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Initialize phase has already run for this entry.</exception>
    public PlatformServiceEntry Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (Initialized)
            throw new InvalidOperationException(
                $"Cannot replace Initialize for '{CategoryName}': the Initialize phase has already run. " +
                "Phase replacements must be selected before the collect (lock-at-collect).");
        _initializationMethod = replacement;
        return this;
    }

    /// <summary>
    /// Runs this domain's Initialize phase, unless it has already run. Lets a caller dot-walk to a
    /// specific domain (e.g. <c>PlatformServices.Connection?.Initialize(host, loggerFactory)</c>) and
    /// initialize it manually, in whatever order matters, before a later
    /// <see cref="PlatformServices.Initialize"/> collect skips anything already done.
    /// </summary>
    public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool defer = false)
    {
        if (_initialize == PhaseState.Ran) return GenericResult<IHost>.Success(host);

        if (defer)
        {
            _initialize = PhaseState.Deferred;
            return GenericResult<IHost>.Success(host);
        }

        var result = _initializationMethod is not null
            ? _initializationMethod(host, loggerFactory)
            : Descriptor.Initialize(host, loggerFactory, true, false);

        _initialize = PhaseState.Ran;
        return result;
    }

    /// <summary>
    /// Runs this domain's Configure phase, unless it has already run. Forwards to <see cref="Descriptor"/>
    /// so a specific domain can be dot-walked directly (e.g.
    /// <c>PlatformServices.Connection?.Configure(builder, loggerFactory)</c>) without reaching through
    /// <see cref="Descriptor"/> explicitly. Idempotent like <see cref="Register"/> and
    /// <see cref="Initialize"/> — a host that configures a domain early, to put it ahead of the others,
    /// is not configured a second time by the later <see cref="PlatformServices.Configure"/> pass.
    /// </summary>
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool defer = false)
    {
        if (_configure == PhaseState.Ran) return GenericResult<IHostApplicationBuilder>.Success(builder);

        if (defer)
        {
            _configure = PhaseState.Deferred;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = _configurationMethod is not null
            ? _configurationMethod(builder, loggerFactory)
            : Descriptor.Configure(builder, loggerFactory, true, false);

        _configure = PhaseState.Ran;
        return result;
    }

    /// <summary>
    /// Runs this domain's Register phase, unless it has already run. Forwards to <see cref="Descriptor"/>
    /// so a specific domain can be dot-walked directly (e.g.
    /// <c>PlatformServices.Connection?.Register(services, loggerFactory)</c>) without reaching through
    /// <see cref="Descriptor"/> explicitly. Idempotent like <see cref="Initialize"/> — a host that
    /// registers a domain explicitly and is then collected by <see cref="PlatformServices.Register"/>
    /// (or vice versa) does not double-register the domain's services.
    /// </summary>
    public IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool defer = false)
    {
        if (_register == PhaseState.Ran) return GenericResult<IHostApplicationBuilder>.Success(builder);

        if (defer)
        {
            _register = PhaseState.Deferred;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = _registrationMethod is not null
            ? _registrationMethod(builder, loggerFactory)
            : Descriptor.Register(builder, loggerFactory, true, false);

        _register = PhaseState.Ran;
        return result;
    }
}
