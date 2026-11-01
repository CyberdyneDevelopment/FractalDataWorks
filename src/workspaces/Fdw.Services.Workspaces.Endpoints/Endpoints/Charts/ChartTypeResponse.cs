using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One chart type a view can be drawn as, with the ways of binding fields to it (FDW-1032).</summary>
public class ChartTypeResponse
{
    /// <summary>Gets or sets the name to send as a view's <c>chartType</c>, for example <c>Bar</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the chart is for.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the ways of binding fields to the chart; a view's bindings must fit exactly one.</summary>
    public List<ChartShapeResponse> Shapes { get; set; } = [];
}
