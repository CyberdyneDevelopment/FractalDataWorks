using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>One way of binding a chart: a set of slots that are bound together.</summary>
/// <remarks>
/// A chart is drawn from exactly one shape. Every required slot of the shape is bound and no slot outside it
/// is. Most chart types have one shape. A map has two, because a place can be one field (a state, a zip code)
/// or a pair (longitude and latitude), and the pair is required together: binding map.x without map.y is not
/// a map.
/// </remarks>
public sealed class ChartBindingShape
{
    /// <summary>Initializes a new instance of the <see cref="ChartBindingShape"/> class.</summary>
    /// <param name="name">What this way of binding is called, for a refusal that has to name it.</param>
    /// <param name="slots">The shape's slots.</param>
    public ChartBindingShape(string name, ChartSlot[] slots)
    {
        Name = name;
        Slots = slots;
    }

    /// <summary>Gets what this way of binding is called.</summary>
    public string Name { get; }

    /// <summary>Gets the shape's slots.</summary>
    public IReadOnlyList<ChartSlot> Slots { get; }
}
