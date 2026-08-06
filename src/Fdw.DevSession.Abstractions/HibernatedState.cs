using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The session is hibernated: a deeper sleep than <see cref="SleepingState"/> in which even its warm
/// context is persisted out and the process footprint is released entirely, to be rehydrated on demand.
/// Reclaimable.
/// </summary>
[TypeOption(typeof(SessionStates), "Hibernated")]
[ExcludeFromCodeCoverage]
public sealed class HibernatedState : SessionStateBase
{
    /// <summary>Initializes a new instance of the <see cref="HibernatedState"/> class.</summary>
    public HibernatedState() : base(3, "Hibernated", isTerminal: false, isReclaimable: true) { }
}
