using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's StageCalculation child configuration (FDW-5287).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetStageCalculation")]
public sealed class GraphDataSetStageCalculationConfigurationCommand : ConfigurationCommandBase<GraphDataSetStageCalculationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageCalculationConfigurationCommand"/> class.</summary>
    public GraphDataSetStageCalculationConfigurationCommand() : base("GraphDataSetStageCalculation") { }
}
