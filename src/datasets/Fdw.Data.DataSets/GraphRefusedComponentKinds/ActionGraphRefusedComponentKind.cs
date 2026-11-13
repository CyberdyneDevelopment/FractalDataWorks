using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>An action (run SQL, run a pipeline, notify, set a variable, read data options into a variable) has an effect and moves no rows; a Graph data set is read for its rows.</summary>
[TypeOption(typeof(GraphRefusedComponentKinds), "Action")]
public sealed class ActionGraphRefusedComponentKind : GraphRefusedComponentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ActionGraphRefusedComponentKind"/> class.</summary>
    public ActionGraphRefusedComponentKind()
        : base("Action", "An action (run SQL, run a pipeline, notify, set a variable, read data options into a variable) has an effect and moves no rows; a Graph data set is read for its rows.", ["ExecuteSql", "ExecutePipeline", "Notify", "SetVariable", "ReadDataOptions"], "GraphDataSetActionRefused")
    {
    }
}
