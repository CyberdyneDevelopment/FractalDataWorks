using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A loop runs the components inside it again; a Graph data set is a graph of row components with no repetition of effects.</summary>
[TypeOption(typeof(GraphRefusedComponentKinds), "Loop")]
public sealed class LoopGraphRefusedComponentKind : GraphRefusedComponentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="LoopGraphRefusedComponentKind"/> class.</summary>
    public LoopGraphRefusedComponentKind()
        : base("Loop", "A loop runs the components inside it again; a Graph data set is a graph of row components with no repetition of effects.", ["DoWhile", "ForEach", "ForLoop"], "GraphDataSetLoopRefused")
    {
    }
}
