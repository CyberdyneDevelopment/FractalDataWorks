using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's Stage child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStage")]
public sealed class GraphDataSetStageConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageConfigurationCommand"/> class.</summary>
    public GraphDataSetStageConfigurationCommand() : base("GraphDataSetStage") { }
}
