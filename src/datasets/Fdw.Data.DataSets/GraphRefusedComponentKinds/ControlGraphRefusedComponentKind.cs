using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A condition, a gate, a switch or a container (FDW-6134) steers which components run and moves no rows; a Graph data set is a graph of row components that are all read.</summary>
[TypeOption(typeof(GraphRefusedComponentKinds), "Control")]
public sealed class ControlGraphRefusedComponentKind : GraphRefusedComponentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ControlGraphRefusedComponentKind"/> class.</summary>
    public ControlGraphRefusedComponentKind()
        : base(
            "Control",
            "A condition, a gate, a switch or a container steers which components run and moves no rows; a Graph data set is a graph of row components that are all read.",
            ["Condition", "Gate", "Switch", "Container"],
            "GraphDataSetControlRefused")
    {
    }
}
