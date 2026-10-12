using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Open collection of dev-session lifecycle states. Uses <c>[MutableTypeCollection]</c> so consumers can
/// register their own states (e.g. a review-specific or approval-specific state) from their own assembly
/// and have them discovered — the built-in set is a starting point, not a closed enumeration.
/// </summary>
[TypeCollection(typeof(SessionStateBase), typeof(ISessionState), typeof(SessionStates))]
public abstract partial class SessionStates : TypeCollectionBase<SessionStateBase, ISessionState>
{
}
