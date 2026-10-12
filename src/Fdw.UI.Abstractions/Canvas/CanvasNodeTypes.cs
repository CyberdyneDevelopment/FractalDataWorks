using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// TypeCollection for canvas node types.
/// </summary>
/// <remarks>
/// <para>
/// Seeded members cover the three canvas use cases (workbench, lineage, calc graph):
/// <list type="bullet">
/// <item><c>Connection</c> — a configured data connection</item>
/// <item><c>DataStore</c> — a logical data store</item>
/// <item><c>DataSet</c> — a named dataset schema</item>
/// <item><c>Calculation</c> — a calculation chain</item>
/// <item><c>Transform</c> — a transformation step</item>
/// <item><c>Pipeline</c> — an ETL/ELT pipeline</item>
/// <item><c>Schedule</c> — a schedule trigger</item>
/// <item><c>CalcInput</c> — an input parameter in a calculation graph</item>
/// <item><c>CalcOperation</c> — an operation step in a calculation graph</item>
/// <item><c>CalcOutput</c> — an output result in a calculation graph</item>
/// </list>
/// </para>
/// <para>
/// Downstream assemblies extend this set by declaring their own <c>[TypeOption]</c> classes
/// that inherit <see cref="CanvasNodeTypeBase"/> — no changes to this file needed.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var nodeType = CanvasNodeTypes.ByName("Pipeline");
/// if (nodeType == CanvasNodeTypes.NotFound)
///     // handle missing node type
///
/// foreach (var t in CanvasNodeTypes.All()) { ... }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(CanvasNodeTypeBase), typeof(ICanvasNodeType), typeof(CanvasNodeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class CanvasNodeTypes : TypeCollectionBase<CanvasNodeTypeBase, ICanvasNodeType>
{
}
