using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// Opt-in, process-global registry and aggregate three-phase entry point for every discovered
/// ServiceTypeCollection. Populated by the <c>[ModuleInitializer]</c> emitted by
/// <c>Fdw.Services.Registration.SourceGenerators</c> when the entry-point assembly loads — an app
/// gets this behavior only if it references that generator; otherwise this type is simply absent
/// from its dependency graph.
/// </summary>
/// <remarks>
/// <para>
/// No name-based lookup surface (no <c>ByName</c>) — each discovered domain gets its own generated,
/// dot-walkable property directly on this class (e.g. <c>PlatformServices.Connection</c>), backed by a
/// private field the generator assigns from <see cref="Add"/>'s return value. There is nothing to look
/// up at read time, so there is no dictionary here at all — just the pending/frozen entry lists.
/// </para>
/// <para>
/// NO FALLBACKS WITHOUT EXPLICIT APPROVAL. <see cref="Add"/> fails loud (throws) rather than silently
/// accepting an inconsistent state.
/// </para>
/// </remarks>
public static class PlatformServices
{
    private static readonly object _gate = new();

    // Pre-freeze staging. Written only by [ModuleInitializer] Add() calls, which are CLR-guaranteed
    // complete before Main() and thus before anything can trigger the freeze.
    private static readonly List<PlatformServiceEntry> _pending = new();

    // Frozen snapshot — built exactly once, lazily, inside EnsureFrozen().
    private static ImmutableArray<PlatformServiceEntry> _frozenOrder;
    private static volatile bool _frozen;

    /// <summary>
    /// Registers a ServiceTypeCollection descriptor under <paramref name="categoryName"/> with its
    /// dependency-depth <paramref name="group"/>, returning the created (or, for a harmless duplicate
    /// re-registration, the existing) entry. Called exclusively from the source-generated
    /// <c>[ModuleInitializer]</c> as the entry-point assembly loads — one call per discovered
    /// <c>[ServiceTypeCollection]</c> domain. The generator assigns the return value to that domain's
    /// own private field, which its generated dot-walkable property reads from directly — no lookup of
    /// any kind happens at read time.
    /// </summary>
    /// <param name="categoryName">Category name (e.g. "Connection") — matches the collection's <c>ServiceCategory</c>.</param>
    /// <param name="serviceCollection">Descriptor exposing the collection's three-phase entry points.</param>
    /// <param name="group">Dependency-depth DAG layer; see <see cref="PlatformServiceEntry.Group"/>.</param>
    /// <param name="manual">
    /// Mirrors <c>[ServiceTypeCollection(Manual = true)]</c> on the collection — set by the generator
    /// from the attribute, never by a hand-written call site (there is no host-side setter). A "declared
    /// choice" domain (e.g. Multitenancy, the auth-server roles) declares this on the attribute so every
    /// host is excluded from the <see cref="Configure"/>/<see cref="Register"/>/<see cref="Initialize"/>
    /// sweeps automatically. The flag stays visible on the returned <see cref="PlatformServiceEntry.Manual"/>
    /// as the indicator that the domain is handled out-of-band, and the host drives it (if at all) by
    /// dot-walking the entry.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The registry is already frozen, or <paramref name="categoryName"/> is already registered to a
    /// <em>different</em> collection type (a real conflict — never silently last-write-wins).
    /// </exception>
    public static PlatformServiceEntry Add(string categoryName, IServiceTypeCollection serviceCollection, int group, bool manual = false)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("categoryName must not be empty.", nameof(categoryName));
        if (serviceCollection is null)
            throw new ArgumentNullException(nameof(serviceCollection));

        lock (_gate)
        {
            if (_frozen)
                throw new InvalidOperationException(
                    $"Cannot add '{categoryName}': PlatformServices is already frozen. Add() may only " +
                    "run from a [ModuleInitializer], which is guaranteed complete before Main() and thus " +
                    "before the first Configure/Register/Initialize/Entries call freezes the registry.");

            var existing = _pending.FirstOrDefault(
                e => string.Equals(e.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                // Why: re-registration of the IDENTICAL CollectionType is harmless (e.g. a
                // deduped-but-still-visible-twice scan result) and must not throw; two DIFFERENT
                // domains claiming the same category name is a real bug and must fail loud.
                if (!ReferenceEquals(existing.Descriptor.CollectionType, serviceCollection.CollectionType))
                    throw new InvalidOperationException(
                        $"Category '{categoryName}' is already registered to " +
                        $"{existing.Descriptor.CollectionType.FullName}; cannot re-register as " +
                        $"{serviceCollection.CollectionType.FullName}.");
                return existing;
            }

            var entry = new PlatformServiceEntry(categoryName, serviceCollection, group) { Manual = manual };
            _pending.Add(entry);
            return entry;
        }
    }

    /// <summary>Enumerates every registered entry, in dependency-safe group order.</summary>
    public static IReadOnlyList<PlatformServiceEntry> Entries()
    {
        EnsureFrozen();
        return _frozenOrder;
    }

    /// <summary>
    /// Calls every registered domain's Configure in dependency-safe order. Replaces the manual,
    /// per-domain <c>XxxServiceTypes.Configure(builder, loggerFactory)</c> calls.
    /// </summary>
    public static IHostApplicationBuilder Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        EnsureFrozen();
        foreach (var entry in _frozenOrder)
        {
            if (entry.Manual) continue;
            builder = entry.Descriptor.Configure(builder, loggerFactory);
        }
        return builder;
    }

    /// <summary>
    /// Calls every registered domain's Register in dependency-safe order — skipping any domain already
    /// registered manually via its own dot-walked entry (e.g.
    /// <c>PlatformServices.Connection?.Register(...)</c>), since <see cref="PlatformServiceEntry.Register"/>
    /// is idempotent. Replaces the manual, per-domain <c>XxxServiceTypes.Register(services, loggerFactory)</c> calls.
    /// </summary>
    public static void Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        EnsureFrozen();
        foreach (var entry in _frozenOrder)
        {
            if (entry.Manual) continue;
            entry.Register(builder, loggerFactory);
        }
    }

    /// <summary>
    /// Calls every registered domain's Initialize in dependency-safe order — skipping any domain
    /// already initialized manually via its own dot-walked entry (e.g.
    /// <c>PlatformServices.Connection?.Initialize(...)</c>), since <see cref="PlatformServiceEntry.Initialize"/>
    /// is idempotent. Replaces the manual, per-domain <c>XxxServiceTypes.Initialize(provider, loggerFactory)</c> calls.
    /// </summary>
    public static void Initialize(IServiceProvider provider, ILoggerFactory? loggerFactory = null)
    {
        EnsureFrozen();
        foreach (var entry in _frozenOrder)
        {
            if (entry.Manual) continue;
            entry.Initialize(provider, loggerFactory);
        }
    }

    /// <summary>
    /// Test-only reset back to the unfrozen, empty state. Never called by production code — the
    /// process-global registry is populated exactly once via module initializers and is not meant to be
    /// re-populated within a process. Internal and only visible to <c>Fdw.Services.Registration.Tests</c>.
    /// </summary>
    internal static void ResetForTesting()
    {
        lock (_gate)
        {
            _pending.Clear();
            _frozenOrder = default;
            _frozen = false;
        }
    }

    // Why lazy-on-first-real-access rather than an explicit Freeze(): every [ModuleInitializer] Add()
    // call is CLR-guaranteed complete before Main() runs, and EnsureFrozen() can only be triggered by
    // code that runs at or after Main() (Program.cs calling one of the methods above) — so there is no
    // window in which EnsureFrozen() could fire before some module initializer's Add() has completed,
    // and the frozen snapshot always reflects every discovered domain with no explicit Freeze() ceremony.
    private static void EnsureFrozen()
    {
        if (_frozen) return;
        lock (_gate)
        {
            if (_frozen) return;
            // Stable sort: ties (same Group) keep _pending's insertion order, which is itself the
            // generator's deterministic alphabetical-by-category emission order.
            _frozenOrder = _pending.OrderBy(e => e.Group).ToImmutableArray();
            _frozen = true;
        }
    }
}
