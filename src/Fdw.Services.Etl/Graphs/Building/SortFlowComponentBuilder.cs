using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Sort tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Sort")]
public sealed class SortFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="SortFlowComponentBuilder"/> class.</summary>
    public SortFlowComponentBuilder() : base("Sort")
    {
    }
}
