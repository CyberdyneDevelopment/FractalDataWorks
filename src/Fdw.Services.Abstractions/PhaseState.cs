namespace Fdw.Services.Abstractions;

/// <summary>
/// Whether one lifecycle phase of a service type or collection still has to run.
/// </summary>
/// <remarks>
/// Three states rather than a bool because PlatformServices and an explicit call read them
/// differently: both skip <see cref="Deferred"/> and <see cref="Ran"/>, but an explicit call RUNS
/// a deferred phase and no-ops a completed one. That distinction is the whole point — it lets a host
/// take one domain out of the collect, let the collect finish, and then run that domain itself.
/// </remarks>
public enum PhaseState
{
    /// <summary>The phase has not run. A collect or an explicit call will run it.</summary>
    NotRun = 0,

    /// <summary>
    /// Claimed by a host that intends to run this phase itself. The collect skips it; the next
    /// explicit call runs it.
    /// </summary>
    Deferred = 1,

    /// <summary>
    /// The phase has run. Nothing runs it again without <c>force</c>.
    /// </summary>
    /// <remarks>
    /// Set even when the phase FAILED: it records that the phase ran, not that it succeeded. Leaving
    /// it unset would let a later collect run it a second time on top of whatever the first attempt
    /// already did.
    /// </remarks>
    Ran = 2,
}
