using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageComputeOutput child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageComputeOutput")]
public sealed class GraphDataSetStageComputeOutputConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageComputeOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageComputeOutputConfigurationCommand"/> class.</summary>
    public GraphDataSetStageComputeOutputConfigurationCommand() : base("GraphDataSetStageComputeOutput") { }
}
