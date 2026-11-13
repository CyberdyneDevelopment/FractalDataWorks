using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageComputeScalar child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageComputeScalar")]
public sealed class GraphDataSetStageComputeScalarConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageComputeScalarConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageComputeScalarConfigurationCommand"/> class.</summary>
    public GraphDataSetStageComputeScalarConfigurationCommand() : base("GraphDataSetStageComputeScalar") { }
}
