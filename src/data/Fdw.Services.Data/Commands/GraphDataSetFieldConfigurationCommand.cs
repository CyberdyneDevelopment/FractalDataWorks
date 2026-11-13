using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's field out child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetField")]
public sealed class GraphDataSetFieldConfigurationCommand : ConfigurationCommandBase<GraphDataSetFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldConfigurationCommand"/> class.</summary>
    public GraphDataSetFieldConfigurationCommand() : base("GraphDataSetField") { }
}
