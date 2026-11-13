using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's link child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetLink")]
public sealed class GraphDataSetLinkConfigurationCommand : ConfigurationCommandBase<GraphDataSetLinkConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetLinkConfigurationCommand"/> class.</summary>
    public GraphDataSetLinkConfigurationCommand() : base("GraphDataSetLink") { }
}
