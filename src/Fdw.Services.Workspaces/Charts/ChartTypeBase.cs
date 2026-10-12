using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Base for the kinds of chart a data view can draw.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type
/// name. A data view stores the option's NAME in <c>ChartType</c>, so the name is the part of this contract
/// that must not change once rows exist. Shapes are an array, not a list: the generated NotFound option calls
/// this constructor with defaults, and only an array has one that is not TC009.
/// </remarks>
public abstract class ChartTypeBase : TypeOptionBase<ChartTypeBase>, IChartType
{
    /// <summary>Initializes a new instance of the <see cref="ChartTypeBase"/> class.</summary>
    /// <param name="name">The chart type name, which is the value persisted.</param>
    /// <param name="description">What the chart is for.</param>
    /// <param name="shapes">The ways the chart can be bound.</param>
    protected ChartTypeBase(string name, string description, ChartBindingShape[] shapes) : base(name, name, description, null)
    {
        Shapes = shapes;
    }

    /// <summary>Gets the ways the chart can be bound.</summary>
    public IReadOnlyList<ChartBindingShape> Shapes { get; }
}
