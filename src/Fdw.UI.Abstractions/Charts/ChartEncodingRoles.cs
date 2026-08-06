using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// TypeCollection for chart encoding roles — the enumerable registry of data-binding channels.
/// </summary>
/// <remarks>
/// <para>
/// Seeded members cover the universal encoding channels shared across all chart types:
/// <list type="bullet">
/// <item><c>X</c> — horizontal axis (spatial)</item>
/// <item><c>Y</c> — vertical axis (spatial)</item>
/// <item><c>Series</c> — grouping dimension for multi-series charts</item>
/// <item><c>Color</c> — colour-coding channel</item>
/// <item><c>Size</c> — bubble/marker size channel (scatter, geo)</item>
/// <item><c>Region</c> — geographic region field (geo charts)</item>
/// <item><c>Latitude</c> — latitude coordinate field (geo charts with lat/long)</item>
/// <item><c>Longitude</c> — longitude coordinate field (geo charts with lat/long)</item>
/// <item><c>Measure</c> — single numeric measure (KPI, donut total)</item>
/// <item><c>Source</c> — source node field for flow charts (Sankey)</item>
/// <item><c>Target</c> — target node field for flow charts (Sankey)</item>
/// <item><c>Weight</c> — flow magnitude for Sankey links</item>
/// <item><c>Tooltip</c> — additional field surfaced in hover tooltips only</item>
/// </list>
/// </para>
/// <para>
/// Downstream assemblies extend this set by declaring their own <c>[TypeOption]</c> classes
/// that inherit <see cref="ChartEncodingRoleBase"/> — no changes to this file needed.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var role = ChartEncodingRoles.ByName("X");
/// if (role == ChartEncodingRoles.NotFound)
///     // fail loud — the role name is not registered
///
/// foreach (var r in ChartEncodingRoles.All()) { ... }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(ChartEncodingRoleBase), typeof(IChartEncodingRole), typeof(ChartEncodingRoles))]
[ExcludeFromCodeCoverage]
public abstract partial class ChartEncodingRoles : TypeCollectionBase<ChartEncodingRoleBase, IChartEncodingRole>
{
}
