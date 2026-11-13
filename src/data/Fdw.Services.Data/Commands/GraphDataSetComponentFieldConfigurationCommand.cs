using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's ComponentField child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetComponentField")]
public sealed class GraphDataSetComponentFieldConfigurationCommand : ConfigurationCommandBase<GraphDataSetComponentFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentFieldConfigurationCommand"/> class.</summary>
    public GraphDataSetComponentFieldConfigurationCommand() : base("GraphDataSetComponentField") { }
}
