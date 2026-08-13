using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fdw.ServiceTypes;

/// <summary>
/// Process registry of which three-phase registration steps have already run, so no step runs twice and a
/// caller can control order by running one step ahead of the collect. Run-state is tracked at two
/// granularities:
/// <list type="bullet">
/// <item><description>the whole service type COLLECTION for a phase (e.g. <c>ConnectionTypes.Register</c>), and</description></item>
/// <item><description>a single OPTION within a collection for a phase (e.g. <c>MsSqlConnectionType</c>'s Register step).</description></item>
/// </list>
/// Each is keyed by the scope the phase operates on — the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>
/// for Configure/Register, the <see cref="IServiceProvider"/> for Initialize — so multiple hosts or test
/// containers in one process stay isolated.
/// </summary>
/// <remarks>
/// Why keyed by the scope object (a <see cref="ConditionalWeakTable{TKey, TValue}"/>) instead of a
/// process-global flag: two hosts (or two test containers) in the same process must EACH get a full
/// registration — a global flag would let the first host's run suppress the second. The weak table also
/// lets the recorded state be collected together with the scope it belongs to.
/// </remarks>
public static class ServiceTypePhaseState
{
    /// <summary>The three registration phases, tracked independently.</summary>
    // Why suppress FDW017: it pushes enums toward TypeCollections for extensibility, but the three-phase
    // pipeline is a CLOSED architectural constant — there are exactly Configure/Register/Initialize and never
    // more — and this value is only ever a HashSet key, never a switch-dispatched domain. A TypeCollection
    // here would be pure ceremony over a fixed triple.
#pragma warning disable FDW017
    public enum Phase
    {
        /// <summary>Phase 1 — Configure (bind options / register required services before Build).</summary>
        Configure,

        /// <summary>Phase 2 — Register (register services before Build).</summary>
        Register,

        /// <summary>Phase 3 — Initialize (wire factories/providers after Build).</summary>
        Initialize,
    }
#pragma warning restore FDW017

    // Why: keyed by the scope object (IServiceCollection or IServiceProvider). The value is the set of steps
    // already run for that scope — (category, phase, optionName?) where optionName == null means the whole
    // collection's phase. HashSet under a lock: registration is single-threaded at startup, but the lock
    // keeps it correct if a host ever parallelizes bootstrap.
    private static readonly ConditionalWeakTable<object, HashSet<(string Category, Phase Phase, string? Option)>> _byScope = new();

    private static HashSet<(string Category, Phase Phase, string? Option)> SetFor(object scope)
        => _byScope.GetValue(scope, static _ => new HashSet<(string Category, Phase Phase, string? Option)>());

    /// <summary>
    /// Atomically records that the whole COLLECTION's <paramref name="phase"/> is running for
    /// <paramref name="scope"/>. Returns <c>true</c> the first time (the caller should run the phase) and
    /// <c>false</c> if it already ran for this scope (the caller should skip).
    /// </summary>
    /// <param name="category">The collection's identity (its class name).</param>
    /// <param name="scope">The IServiceCollection (Configure/Register) or IServiceProvider (Initialize) the phase runs against.</param>
    /// <param name="phase">The phase being run.</param>
    /// <exception cref="ArgumentException"><paramref name="category"/> is null/empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is null.</exception>
    // Why fail loud on a missing scope/category: an unkeyed or unnamed step is a wiring bug, never a
    // silently-allowed re-run — see the FDW no-fallbacks rule.
    public static bool TryMarkCollection(string category, object scope, Phase phase)
    {
        if (string.IsNullOrEmpty(category)) throw new ArgumentException("category must not be empty.", nameof(category));
        if (scope is null) throw new ArgumentNullException(nameof(scope));

        var set = SetFor(scope);
        lock (set) return set.Add((category, phase, null));
    }

    /// <summary>
    /// Atomically records that a single OPTION's <paramref name="phase"/> is running within
    /// <paramref name="category"/> for <paramref name="scope"/>. Returns <c>true</c> the first time (run the
    /// option's step) and <c>false</c> if it already ran for this scope (skip it).
    /// </summary>
    /// <param name="category">The owning collection's identity (its class name).</param>
    /// <param name="optionName">The option's name.</param>
    /// <param name="scope">The IServiceCollection (Configure/Register) or IServiceProvider (Initialize) the phase runs against.</param>
    /// <param name="phase">The phase being run.</param>
    /// <exception cref="ArgumentException"><paramref name="category"/> or <paramref name="optionName"/> is null/empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is null.</exception>
    public static bool TryMarkOption(string category, string optionName, object scope, Phase phase)
    {
        if (string.IsNullOrEmpty(category)) throw new ArgumentException("category must not be empty.", nameof(category));
        if (string.IsNullOrEmpty(optionName)) throw new ArgumentException("optionName must not be empty.", nameof(optionName));
        if (scope is null) throw new ArgumentNullException(nameof(scope));

        var set = SetFor(scope);
        lock (set) return set.Add((category, phase, optionName));
    }

    /// <summary>
    /// Returns whether the whole COLLECTION's <paramref name="phase"/> has already run for
    /// <paramref name="scope"/>, without recording anything. Use for read-only inspection (e.g. tests).
    /// </summary>
    public static bool HasCollectionRun(string category, object scope, Phase phase)
    {
        if (string.IsNullOrEmpty(category)) throw new ArgumentException("category must not be empty.", nameof(category));
        if (scope is null) throw new ArgumentNullException(nameof(scope));

        var set = SetFor(scope);
        lock (set) return set.Contains((category, phase, null));
    }
}
