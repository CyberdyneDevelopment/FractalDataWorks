using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Combines the rows of several inputs into one output, in the order they arrive.</summary>
[TypeOption(typeof(ComponentTypes), "UnionAll")]
public sealed class UnionAllComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UnionAllComponentType"/> class.</summary>
    public UnionAllComponentType() : base(
        "UnionAll",
        "Union all",
        "Combines the rows of several inputs into one output, in the order they arrive.",
        "Combine",
        [
            new FlowNodeSpec("input", true, 2, null, false, "One of the streams combined."),
        ],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "The rows the component produces."),
        ],
        [])
    {
    }
}
