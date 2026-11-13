using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageLookup child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageLookup")]
public sealed class GraphDataSetStageLookupConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageLookupConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageLookupConfigurationCommand"/> class.</summary>
    public GraphDataSetStageLookupConfigurationCommand() : base("GraphDataSetStageLookup") { }
}
