using Fdw.Collections;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Whether one lifecycle phase of a service type or collection still has to run (FDW-4149).
/// </summary>
/// <remarks>
/// Three states rather than a bool because PlatformServices and an explicit call read them differently: both skip a
/// <see cref="IsDeferred"/> and a <see cref="HasRun"/> phase in a collect, but an explicit call RUNS a deferred phase and no-ops a
/// completed one. That distinction is the whole point: it lets a host take one domain out of the collect, let the collect finish, and then run
/// that domain itself.
/// </remarks>
public interface IPhaseState : ITypeOption<int, IPhaseState>
{
    /// <summary>Gets a value indicating whether the phase has run. Set even when the phase FAILED: it records that the phase ran, not that it succeeded.</summary>
    bool HasRun { get; }

    /// <summary>Gets a value indicating whether a host claimed the phase to run itself: the collect skips it and the next explicit call runs it.</summary>
    bool IsDeferred { get; }
}
