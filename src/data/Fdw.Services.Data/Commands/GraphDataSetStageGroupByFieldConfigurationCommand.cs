using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageGroupByField child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageGroupByField")]
public sealed class GraphDataSetStageGroupByFieldConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageGroupByFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageGroupByFieldConfigurationCommand"/> class.</summary>
    public GraphDataSetStageGroupByFieldConfigurationCommand() : base("GraphDataSetStageGroupByField") { }
}
