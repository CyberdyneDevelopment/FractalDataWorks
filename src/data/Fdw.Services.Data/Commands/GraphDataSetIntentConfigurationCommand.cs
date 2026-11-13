using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's intended source child configuration (FDW-5294).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetIntent")]
public sealed class GraphDataSetIntentConfigurationCommand : ConfigurationCommandBase<GraphDataSetIntentConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntentConfigurationCommand"/> class.</summary>
    public GraphDataSetIntentConfigurationCommand() : base("GraphDataSetIntent") { }
}
