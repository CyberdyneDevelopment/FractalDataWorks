using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The families of component kind a Graph data set's graph refuses (FDW-5287): a destination, an action and a loop. A graph is read, not run for
/// its effects, so none of them has a place in it. One list of names for the check, which never types a name of its own.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GraphRefusedComponentKindBase), typeof(IGraphRefusedComponentKind), typeof(GraphRefusedComponentKinds))]
public abstract partial class GraphRefusedComponentKinds : TypeCollectionBase<GraphRefusedComponentKindBase, IGraphRefusedComponentKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
