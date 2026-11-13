using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageSortField child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageSortField")]
public sealed class GraphDataSetStageSortFieldConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageSortFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageSortFieldConfigurationCommand"/> class.</summary>
    public GraphDataSetStageSortFieldConfigurationCommand() : base("GraphDataSetStageSortField") { }
}
