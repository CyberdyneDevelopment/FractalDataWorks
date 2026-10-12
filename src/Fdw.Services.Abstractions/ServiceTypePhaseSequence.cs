using System.Threading;

namespace Fdw.Collections;

/// <summary>
/// The run-order bookkeeping behind the phase-invocation log lines: which collection is running a
/// given phase, and which option within it.
/// </summary>
/// <remarks>
/// <para>
/// Why this is a separate object rather than statics on the bases: static state on a generic base is
/// per-closed-generic, so a counter declared on <c>ServiceTypeCollectionBase&lt;TBase,TInterface&gt;</c>
/// would restart at 1 for every collection and answer nothing. The order a reader wants — "Connections
/// registered fourth, and MsSql was the second option inside it" — spans collections, so the counter
/// has to live somewhere non-generic that every closed generic shares.
/// </para>
/// <para>
/// One instance per phase, because the three phases run as three separate passes over the same
/// collections: Configure numbers its own pass, Register numbers its own, Initialize its own.
/// </para>
/// </remarks>
internal sealed class ServiceTypePhaseSequence
{
    private int _collection;
    private int _option;

    /// <summary>Gets the phase-1 sequence.</summary>
    internal static ServiceTypePhaseSequence Configure { get; } = new();

    /// <summary>Gets the phase-2 sequence.</summary>
    internal static ServiceTypePhaseSequence Register { get; } = new();

    /// <summary>Gets the phase-3 sequence.</summary>
    internal static ServiceTypePhaseSequence Initialize { get; } = new();

    /// <summary>
    /// Gets the collection currently running this phase, which the option-level lines name so an
    /// ordinal like "#2" is anchored to something.
    /// </summary>
    /// <remarks>
    /// An empty value is meaningful rather than missing: it says an option's phase ran without a
    /// collection collect around it — a direct invocation off the supported path — and that is exactly
    /// what the reader of such a line needs to know.
    /// </remarks>
    internal string CurrentCollectionName { get; private set; } = string.Empty;

    /// <summary>
    /// Opens a collection's pass through this phase: claims the next collection number and restarts
    /// option numbering, so options are numbered within their collection rather than continuously.
    /// </summary>
    /// <param name="collectionName">The collection beginning this phase.</param>
    /// <returns>This collection's position in the phase.</returns>
    internal int BeginCollection(string collectionName)
    {
        CurrentCollectionName = collectionName;
        Interlocked.Exchange(ref _option, 0);
        return Interlocked.Increment(ref _collection);
    }

    /// <summary>Claims the next option position within the collection currently running.</summary>
    /// <returns>The option's position within its collection.</returns>
    internal int NextOption() => Interlocked.Increment(ref _option);
}
