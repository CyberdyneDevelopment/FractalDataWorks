using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageCheck child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageCheck")]
public sealed class GraphDataSetStageCheckConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageCheckConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageCheckConfigurationCommand"/> class.</summary>
    public GraphDataSetStageCheckConfigurationCommand() : base("GraphDataSetStageCheck") { }
}
