using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Open collection of strand lifecycle states. Uses <c>[MutableTypeCollection]</c> so consumers can
/// register their own strand states from their own assembly and have them discovered — the built-in set
/// is a starting point, not a closed enumeration.
/// </summary>
[TypeCollection(typeof(StrandStateBase), typeof(IStrandState), typeof(StrandStates))]
public abstract partial class StrandStates : TypeCollectionBase<StrandStateBase, IStrandState>
{
}
