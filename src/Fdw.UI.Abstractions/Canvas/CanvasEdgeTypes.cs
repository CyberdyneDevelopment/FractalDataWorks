using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// TypeCollection for canvas edge types.
/// </summary>
/// <remarks>
/// <para>
/// Seeded members:
/// <list type="bullet">
/// <item><c>Flow</c> — primary data or control flow between nodes (pipeline wiring)</item>
/// <item><c>Reference</c> — a dependency or reference relationship (lineage)</item>
/// <item><c>FieldMapping</c> — a field-level data mapping between ports</item>
/// </list>
/// </para>
/// <para>
/// Downstream assemblies extend this set by declaring their own <c>[TypeOption]</c> classes
/// that inherit <see cref="CanvasEdgeTypeBase"/>.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var edgeType = CanvasEdgeTypes.ByName("Flow");
/// if (edgeType == CanvasEdgeTypes.NotFound)
///     // handle missing
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(CanvasEdgeTypeBase), typeof(ICanvasEdgeType), typeof(CanvasEdgeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class CanvasEdgeTypes : TypeCollectionBase<CanvasEdgeTypeBase, ICanvasEdgeType>
{
}
