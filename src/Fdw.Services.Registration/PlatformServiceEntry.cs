using System;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// One registered ServiceTypeCollection domain plus its dependency-depth group and Initialize tracking.
/// </summary>
/// <param name="CategoryName">Category name (e.g. "Connection") — matches the collection's <c>ServiceCategory</c>.</param>
/// <param name="Descriptor">Descriptor exposing the collection's Configure/Register/Initialize entry points.</param>
/// <param name="Group">
/// Dependency-depth DAG layer: 0 if the domain depends on no other domain; otherwise
/// 1 + the maximum group of every domain it depends on. Domains sharing a group have no
/// dependency relationship between them (by construction of the graph), so their relative order
/// within a group is provably safe regardless of arrangement.
/// </param>
/// <remarks>
/// <see cref="Group"/> is immutable-by-replace: "changing" it is always <c>entry with { Group = newValue }</c>,
/// never in-place mutation — this keeps the frozen snapshot's sort order genuinely immutable with no back
/// door. <see cref="Configured"/>, <see cref="Initialized"/> and <see cref="Registered"/> are deliberately
/// the exceptions: they are pure bookkeeping that never affects sort/graph correctness, so they are mutated
/// in place — the entry is a reference type, so flipping any of them on one shared instance is visible
/// everywhere that instance is held, with no array rebuild.
/// </remarks>
public sealed record PlatformServiceEntry(string CategoryName, IServiceTypeCollection Descriptor, int Group)
{
    /// <summary>Whether <see cref="Configure"/> has already run for this entry.</summary>
    public bool Configured { get; private set; }

    /// <summary>Whether <see cref="Initialize"/> has already run for this entry.</summary>
    public bool Initialized { get; private set; }

    /// <summary>Whether <see cref="Register"/> has already run for this entry.</summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Whether this domain is excluded from the <see cref="PlatformServices"/> sweeps
    /// (<see cref="PlatformServices.Configure"/>/<see cref="PlatformServices.Register"/>/
    /// <see cref="PlatformServices.Initialize"/>) and driven manually by the host instead.
    /// </summary>
    /// <remarks>
    /// Why: "declared choice" domains (e.g. Multitenancy, the auth-server roles) have multiple options
    /// registering the SAME interfaces — a blanket sweep would leave the winner to module-initializer
    /// discovery order. The domain declares itself manual via <c>[ServiceTypeCollection(Manual = true)]</c>
    /// (there is no host-side setter); a host reads this indicator to see the domain is handled
    /// out-of-band and drives exactly ONE option by its configured name. Set once at construction.
    /// </remarks>
    public bool Manual { get; init; }

    // ── Phase-delegate replacements (author-curated variant selection; the keyset stays frozen) ─────────
    // Why: the frozen registry locks the SET of entries (no add/remove), but an entry is a mutable
    // reference type — exactly as Initialized/Registered are mutated post-freeze. That lets a host SELECT
    // an alternative phase delegate BEFORE the sweep runs, without touching discovery determinism. The
    // BLESSED use is an author-curated named variant (a collection's own UseXxx() calls the matching
    // gerund setter with a delegate the AUTHOR wrote — keeps registration FDW-owned); the raw setter is
    // the documented escape hatch. Each replacement is locked once its phase has run (lock-at-sweep), so
    // behaviour is deterministic from the first sweep onward.
    //
    // Why the gerund and not OverrideXxx: `override` means virtual dispatch in C#, and this is not that —
    // it swaps a delegate. The same word is already spoken for one level down, where ServiceTypeBase
    // documents overriding the INVOKER as the way to add wiring a replacement cannot clobber. Naming both
    // "override" would put one word on the two operations that must not be confused.
    private Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>>? _configurationMethod;
    private Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>>? _registrationMethod;
    private Func<IHost, ILoggerFactory?, IGenericResult<IHost>>? _initializationMethod;

    /// <summary>
    /// Selects an alternative Configure phase delegate for this entry, replacing the descriptor default.
    /// Intended for a collection author to expose named variants (e.g. <c>UseThinClient()</c> that forwards
    /// an author-written delegate here); the raw setter is the documented escape hatch. Must be called
    /// BEFORE the Configure sweep runs for this entry. Returns <c>this</c> for fluent chaining.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Configure phase has already run for this entry.</exception>
    public PlatformServiceEntry Configuration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (Configured)
            throw new InvalidOperationException(
                $"Cannot replace Configure for '{CategoryName}': the Configure phase has already run. " +
                "Phase replacements must be selected before the sweep (lock-at-sweep).");
        _configurationMethod = replacement;
        return this;
    }

    /// <summary>
    /// Selects an alternative Register phase delegate for this entry, replacing the descriptor default.
    /// See <see cref="Configuration"/> for intended (author-variant) vs escape-hatch usage. Must be
    /// called BEFORE the Register sweep runs for this entry. Returns <c>this</c> for fluent chaining.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Register phase has already run for this entry.</exception>
    public PlatformServiceEntry Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (Registered)
            throw new InvalidOperationException(
                $"Cannot replace Register for '{CategoryName}': the Register phase has already run. " +
                "Phase replacements must be selected before the sweep (lock-at-sweep).");
        _registrationMethod = replacement;
        return this;
    }

    /// <summary>
    /// Selects an alternative Initialize phase delegate for this entry, replacing the descriptor default.
    /// See <see cref="Configuration"/> for intended (author-variant) vs escape-hatch usage. Must be
    /// called BEFORE the Initialize sweep runs for this entry. Returns <c>this</c> for fluent chaining.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Initialize phase has already run for this entry.</exception>
    public PlatformServiceEntry Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> replacement)
    {
        if (replacement is null) throw new ArgumentNullException(nameof(replacement));
        if (Initialized)
            throw new InvalidOperationException(
                $"Cannot replace Initialize for '{CategoryName}': the Initialize phase has already run. " +
                "Phase replacements must be selected before the sweep (lock-at-sweep).");
        _initializationMethod = replacement;
        return this;
    }

    /// <summary>
    /// Runs this domain's Initialize phase, unless it has already run. Lets a caller dot-walk to a
    /// specific domain (e.g. <c>PlatformServices.Connection?.Initialize(host, loggerFactory)</c>) and
    /// initialize it manually, in whatever order matters, before a later
    /// <see cref="PlatformServices.Initialize"/> sweep skips anything already done.
    /// </summary>
    // Why the flag is set even when the phase failed: it records that this domain's Initialize HAS
    // RUN, not that it succeeded. Leaving it unset would let a later sweep run it a second time on top
    // of whatever the first attempt already did — double-initializing the part that worked in order to
    // retry the part that did not. The failure goes to the caller; the re-run does not happen.
    public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
    {
        if (Initialized) return GenericResult<IHost>.Success(host);

        var result = _initializationMethod is not null
            ? _initializationMethod(host, loggerFactory)
            : Descriptor.Initialize(host, loggerFactory, false);

        Initialized = true;
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
    // Why the flag is set even when the phase failed: see Initialize — it records that the phase ran.
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        if (Configured) return GenericResult<IHostApplicationBuilder>.Success(builder);

        var result = _configurationMethod is not null
            ? _configurationMethod(builder, loggerFactory)
            : Descriptor.Configure(builder, loggerFactory, false);

        Configured = true;
        return result;
    }

    /// <summary>
    /// Runs this domain's Register phase, unless it has already run. Forwards to <see cref="Descriptor"/>
    /// so a specific domain can be dot-walked directly (e.g.
    /// <c>PlatformServices.Connection?.Register(services, loggerFactory)</c>) without reaching through
    /// <see cref="Descriptor"/> explicitly. Idempotent like <see cref="Initialize"/> — a host that
    /// registers a <c>Manual</c> domain explicitly and is then swept by <see cref="PlatformServices.Register"/>
    /// (or vice versa) does not double-register the domain's services.
    /// </summary>
    // Why the flag is set even when the phase failed: see Initialize — it records that the phase ran.
    public IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        if (Registered) return GenericResult<IHostApplicationBuilder>.Success(builder);

        var result = _registrationMethod is not null
            ? _registrationMethod(builder, loggerFactory)
            : Descriptor.Register(builder, loggerFactory, false);

        Registered = true;
        return result;
    }
}
