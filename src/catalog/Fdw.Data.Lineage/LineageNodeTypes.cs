using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage;

/// <summary>
/// Collection of lineage node types.
/// </summary>
[TypeCollection(typeof(LineageNodeTypeBase), typeof(ILineageNodeType), typeof(LineageNodeTypes))]
public abstract partial class LineageNodeTypes : TypeCollectionBase<LineageNodeTypeBase, ILineageNodeType>
{
}
