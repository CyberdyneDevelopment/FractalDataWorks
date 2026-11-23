using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's ComponentSetting child configuration (FDW-6133).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetComponentSetting")]
public sealed class GraphDataSetComponentSettingConfigurationCommand : ConfigurationCommandBase<GraphDataSetComponentSettingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentSettingConfigurationCommand"/> class.</summary>
    public GraphDataSetComponentSettingConfigurationCommand() : base("GraphDataSetComponentSetting") { }
}
