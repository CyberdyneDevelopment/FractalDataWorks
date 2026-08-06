using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline transform calculations.
/// Targets the pipe.CalculationOperationConfiguration table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformCalculationConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting a calculation.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "CalculationOperationConfiguration")]
public sealed class PipelineTransformCalculationConfigurationCommand : ConfigurationCommandBase<PipelineTransformCalculationConfiguration>
{
    /// <summary>Initializes the command targeting the CalculationOperationConfiguration table.</summary>
    public PipelineTransformCalculationConfigurationCommand() : base("CalculationOperationConfiguration") { }
}
