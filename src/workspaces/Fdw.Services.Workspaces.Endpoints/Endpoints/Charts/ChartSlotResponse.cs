using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One place a chart takes a field, as <c>GET /chart-types</c> describes it.</summary>
public class ChartSlotResponse
{
    /// <summary>Gets or sets the slot's name in a view's <c>encoding</c> bindings, for example <c>category</c> or <c>map.x</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the axes the slot accepts (<c>Dimension</c>, <c>Measure</c>, <c>Time</c>, <c>Place</c>). A field may be bound when its
    /// datum type's <c>axes</c> (from <c>GET /datum-types</c>) include one of them. A slot that accepts <c>Measure</c> also takes
    /// <c>{"field", "aggregate"}</c>, with an aggregate the field's datum type allows.
    /// </summary>
    public List<string> Axes { get; set; } = [];

    /// <summary>Gets or sets whether the chart cannot be drawn without this slot bound.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the slot takes more than one field.</summary>
    public bool AllowsMany { get; set; }
}
