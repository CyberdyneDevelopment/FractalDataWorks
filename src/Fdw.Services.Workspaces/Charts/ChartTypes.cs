using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>
/// The kinds of chart a data view can draw, and for each the slots it binds fields to (FDW-1023).
/// </summary>
/// <remarks>
/// The platform owns the mechanism, never the values: a chart type is a set of named slots and the kinds of
/// field each accepts, nothing about how it is drawn. How a chart looks belongs to the console. A data view
/// stores a chart type's name in <c>ChartType</c> and its bindings in <c>Encoding</c>; creating one refuses a
/// name this collection does not have and a binding that does not fit its type.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ChartTypeBase), typeof(IChartType), typeof(ChartTypes))]
public abstract partial class ChartTypes : TypeCollectionBase<ChartTypeBase, IChartType>
{
}
