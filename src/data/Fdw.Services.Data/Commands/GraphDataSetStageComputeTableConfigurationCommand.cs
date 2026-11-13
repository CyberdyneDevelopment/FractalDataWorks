using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageComputeTable child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageComputeTable")]
public sealed class GraphDataSetStageComputeTableConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageComputeTableConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageComputeTableConfigurationCommand"/> class.</summary>
    public GraphDataSetStageComputeTableConfigurationCommand() : base("GraphDataSetStageComputeTable") { }
}
