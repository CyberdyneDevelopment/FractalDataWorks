using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's ComponentColumn child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetComponentColumn")]
public sealed class GraphDataSetComponentColumnConfigurationCommand : ConfigurationCommandBase<GraphDataSetComponentColumnConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentColumnConfigurationCommand"/> class.</summary>
    public GraphDataSetComponentColumnConfigurationCommand() : base("GraphDataSetComponentColumn") { }
}
