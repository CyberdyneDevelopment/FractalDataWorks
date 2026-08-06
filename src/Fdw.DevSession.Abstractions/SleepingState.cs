using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The session is asleep: its warm in-memory resources are freed but its record and isolated copy are
/// retained, so it wakes cheaply. Reclaimable.
/// </summary>
[TypeOption(typeof(SessionStates), "Sleeping")]
[ExcludeFromCodeCoverage]
public sealed class SleepingState : SessionStateBase
{
    /// <summary>Initializes a new instance of the <see cref="SleepingState"/> class.</summary>
    public SleepingState() : base(2, "Sleeping", isTerminal: false, isReclaimable: true) { }
}
