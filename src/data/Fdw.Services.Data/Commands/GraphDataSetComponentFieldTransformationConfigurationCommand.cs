using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's ComponentFieldTransformation child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetComponentFieldTransformation")]
public sealed class GraphDataSetComponentFieldTransformationConfigurationCommand : ConfigurationCommandBase<GraphDataSetComponentFieldTransformationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentFieldTransformationConfigurationCommand"/> class.</summary>
    public GraphDataSetComponentFieldTransformationConfigurationCommand() : base("GraphDataSetComponentFieldTransformation") { }
}
