using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's ComponentSplitOutput child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetComponentSplitOutput")]
public sealed class GraphDataSetComponentSplitOutputConfigurationCommand : ConfigurationCommandBase<GraphDataSetComponentSplitOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentSplitOutputConfigurationCommand"/> class.</summary>
    public GraphDataSetComponentSplitOutputConfigurationCommand() : base("GraphDataSetComponentSplitOutput") { }
}
