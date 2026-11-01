using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One way of binding a chart: the slots it takes, as <c>GET /chart-types</c> describes it.</summary>
/// <remarks>A chart type with several shapes (a map binds longitude and latitude, or one place) is bound by exactly one of them.</remarks>
public class ChartShapeResponse
{
    /// <summary>Gets or sets the shape's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the slots the shape takes.</summary>
    public List<ChartSlotResponse> Slots { get; set; } = [];
}
