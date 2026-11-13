using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageComputeInput child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageComputeInput")]
public sealed class GraphDataSetStageComputeInputConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageComputeInputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageComputeInputConfigurationCommand"/> class.</summary>
    public GraphDataSetStageComputeInputConfigurationCommand() : base("GraphDataSetStageComputeInput") { }
}
