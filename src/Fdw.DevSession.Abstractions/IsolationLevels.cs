using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Open collection of dev-session isolation strategies. Uses <c>[MutableTypeCollection]</c> so that
/// consumers can register their own strategy (e.g. a container-clone or fork variant) from their own
/// assembly and have it discovered — the built-in set is a starting point, not a closed enumeration.
/// </summary>
[TypeCollection(typeof(IsolationLevelBase), typeof(IIsolationLevel), typeof(IsolationLevels))]
public abstract partial class IsolationLevels : TypeCollectionBase<IsolationLevelBase, IIsolationLevel>
{
}
