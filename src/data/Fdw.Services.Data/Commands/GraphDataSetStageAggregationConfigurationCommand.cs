using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageAggregation child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageAggregation")]
public sealed class GraphDataSetStageAggregationConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageAggregationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageAggregationConfigurationCommand"/> class.</summary>
    public GraphDataSetStageAggregationConfigurationCommand() : base("GraphDataSetStageAggregation") { }
}
