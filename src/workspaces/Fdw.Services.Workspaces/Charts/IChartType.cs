using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>A kind of chart a data view can draw.</summary>
public interface IChartType : ITypeOption<int, ChartTypeBase>
{
    /// <summary>Gets what the chart is for.</summary>
    string Description { get; }

    /// <summary>Gets the ways a chart of this type can be bound; exactly one is used.</summary>
    IReadOnlyList<ChartBindingShape> Shapes { get; }
}
