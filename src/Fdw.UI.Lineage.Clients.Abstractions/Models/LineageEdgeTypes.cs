using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// TypeCollection for lineage edge types.
/// </summary>
[TypeCollection(typeof(LineageEdgeTypeBase), typeof(ILineageEdgeType), typeof(LineageEdgeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class LineageEdgeTypes : TypeCollectionBase<LineageEdgeTypeBase, ILineageEdgeType> { }
