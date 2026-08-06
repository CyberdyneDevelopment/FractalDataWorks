using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// TypeCollection for lineage node types.
/// </summary>
[TypeCollection(typeof(LineageNodeTypeBase), typeof(ILineageNodeType), typeof(LineageNodeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class LineageNodeTypes : TypeCollectionBase<LineageNodeTypeBase, ILineageNodeType> { }
