using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage;

/// <summary>
/// Collection of lineage edge types.
/// </summary>
[TypeCollection(typeof(LineageEdgeTypeBase), typeof(ILineageEdgeType), typeof(LineageEdgeTypes))]
public abstract partial class LineageEdgeTypes : TypeCollectionBase<LineageEdgeTypeBase, ILineageEdgeType>
{
}
