using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage;

/// <summary>
/// Collection of lineage node status types.
/// </summary>
[TypeCollection(typeof(LineageNodeStatusBase), typeof(ILineageNodeStatus), typeof(LineageNodeStatuses))]
public abstract partial class LineageNodeStatuses : TypeCollectionBase<LineageNodeStatusBase, ILineageNodeStatus>
{
}
