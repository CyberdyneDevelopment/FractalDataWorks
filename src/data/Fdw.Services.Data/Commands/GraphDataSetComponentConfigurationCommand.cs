using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's component child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetComponent")]
public sealed class GraphDataSetComponentConfigurationCommand : ConfigurationCommandBase<GraphDataSetComponentConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentConfigurationCommand"/> class.</summary>
    public GraphDataSetComponentConfigurationCommand() : base("GraphDataSetComponent") { }
}
