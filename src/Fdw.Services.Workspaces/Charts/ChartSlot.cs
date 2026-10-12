using System.Collections.Generic;
using Fdw.Schema;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>One place a chart takes a field: a name, the axes it accepts, whether it is needed, and whether it takes several.</summary>
public sealed class ChartSlot
{
    /// <summary>Initializes a new instance of the <see cref="ChartSlot"/> class.</summary>
    /// <param name="name">
    /// The slot's name in a saved chart's bindings. A dotted name (map.x) belongs to a group of slots that
    /// are bound together; the group is stated by the shape that lists them.
    /// </param>
    /// <param name="axes">Names of the <see cref="AxisTypes"/> options the slot accepts; a field that sits on any of them may be bound. A slot that accepts Measure also takes an aggregate of a field.</param>
    /// <param name="isRequired">Whether the chart cannot draw without this slot bound.</param>
    /// <param name="allowsMany">Whether the slot takes more than one field (several values on one axis).</param>
    public ChartSlot(string name, string[] axes, bool isRequired, bool allowsMany)
    {
        Name = name;
        Axes = axes;
        IsRequired = isRequired;
        AllowsMany = allowsMany;
    }

    /// <summary>Gets the slot's name in a saved chart's bindings.</summary>
    public string Name { get; }

    /// <summary>Gets the names of the axes the slot accepts.</summary>
    public IReadOnlyList<string> Axes { get; }

    /// <summary>Gets whether the chart cannot draw without this slot bound.</summary>
    public bool IsRequired { get; }

    /// <summary>Gets whether the slot takes more than one field.</summary>
    public bool AllowsMany { get; }
}
