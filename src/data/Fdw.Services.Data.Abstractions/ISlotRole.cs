using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Whether this copy of the app may do its background work now (FDW-5846).
/// </summary>
/// <remarks>
/// An app on A/B slots runs twice at once. Both copies answer requests; only the live copy does what nobody asked for
/// (timers, queues, watchers, schedulers), because the standby copy shares the world with it. The role is read on every
/// call, not once at start: a swap changes a running copy's role. A host that does not run on a slot answers that its
/// work may run, as it always has; a host that runs on a slot and cannot say which role it has answers with a failure,
/// and its work does not run.
/// </remarks>
public interface ISlotRole
{
    /// <summary>Reads the role this copy has now.</summary>
    /// <returns>
    /// <see langword="true"/> when the host is not on a slot or its role file says <c>live</c>; <see langword="false"/> when the role file says
    /// <c>standby</c>; otherwise a failure that names the file and what it holds.
    /// </returns>
    IGenericResult<bool> BackgroundWorkMayRun();

    /// <summary>
    /// Gets the slot's own name, as the slot file states it (<c>a</c> or <c>b</c>), or <see langword="null"/> when this copy does not run on a slot or its slot file states none (FDW-6519).
    /// </summary>
    /// <remarks>
    /// Stated once, by the one reader of the slot file, so a run's record can say which copy ran it (both copies always run and the live role swaps). Absence is
    /// absence: a host off a slot has no slot name, and none is made up. The value is the file's own, never mapped or prettified.
    /// </remarks>
    string? SlotName { get; }
}
