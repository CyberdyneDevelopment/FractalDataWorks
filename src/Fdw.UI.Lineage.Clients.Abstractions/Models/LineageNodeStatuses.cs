using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// TypeCollection for lineage node statuses.
/// </summary>
[TypeCollection(typeof(LineageNodeStatusBase), typeof(ILineageNodeStatus), typeof(LineageNodeStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class LineageNodeStatuses : TypeCollectionBase<LineageNodeStatusBase, ILineageNodeStatus> { }
