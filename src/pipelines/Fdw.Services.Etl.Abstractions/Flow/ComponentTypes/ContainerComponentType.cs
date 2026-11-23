using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Groups the components inside it and runs them once; a container may hold containers, to the depth the host allows (FDW-6134).</summary>
[TypeOption(typeof(ComponentTypes), "Container")]
public sealed class ContainerComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerComponentType"/> class.</summary>
    public ContainerComponentType() : base(
        "Container",
        "Container",
        "Groups the components inside it and runs them once. A line from a decision's output into it runs it only when that output fired.",
        "Control flow",
        true,
        [])
    {
    }
}
