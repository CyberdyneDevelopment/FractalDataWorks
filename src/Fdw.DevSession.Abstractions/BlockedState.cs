using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The session is blocked awaiting an external actor — typically a human reviewer on a submitted
/// merge request. Because progress is durable and the wait is out of the session's control, a blocked
/// session is reclaimable: this is the natural point to free warm resources.
/// </summary>
[TypeOption(typeof(SessionStates), "Blocked")]
[ExcludeFromCodeCoverage]
public sealed class BlockedState : SessionStateBase
{
    /// <summary>Initializes a new instance of the <see cref="BlockedState"/> class.</summary>
    public BlockedState() : base(4, "Blocked", isTerminal: false, isReclaimable: true) { }
}
