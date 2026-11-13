using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A destination writes rows somewhere; a Graph data set is read and stores nothing.</summary>
[TypeOption(typeof(GraphRefusedComponentKinds), "Destination")]
public sealed class DestinationGraphRefusedComponentKind : GraphRefusedComponentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DestinationGraphRefusedComponentKind"/> class.</summary>
    public DestinationGraphRefusedComponentKind()
        : base("Destination", "A destination writes rows somewhere; a Graph data set is read and stores nothing.", ["ContainerDestination", "DataSetDestination"], "GraphDataSetDestinationRefused")
    {
    }
}
