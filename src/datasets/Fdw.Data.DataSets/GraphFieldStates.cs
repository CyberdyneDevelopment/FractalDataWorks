using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>The states of a field out of a Graph data set: Question open, Intended and Defined (FDW-5294).</summary>
/// <remarks>Each option carries its own test and its precedence; <see cref="GraphDataSetFieldStates"/> picks the one that applies.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GraphFieldStateBase), typeof(IGraphFieldState), typeof(GraphFieldStates))]
public abstract partial class GraphFieldStates : TypeCollectionBase<GraphFieldStateBase, IGraphFieldState>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
