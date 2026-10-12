using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Open collection of strand handlers the coordinator routes to. Uses <c>[MutableTypeCollection]</c> and
/// ships with no built-in options: a strand handler is domain-specific work, so consumers register their
/// own from their own assemblies. The framework owns only the routing mechanism, never the set of handlers.
/// </summary>
[TypeCollection(typeof(StrandHandlerBase), typeof(IStrandHandler), typeof(StrandHandlers))]
public abstract partial class StrandHandlers : TypeCollectionBase<StrandHandlerBase, IStrandHandler>
{
}
