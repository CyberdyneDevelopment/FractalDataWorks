using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline transform aggregations.
/// Targets the pipe.AggregationOperationConfiguration table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformAggregationConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting an aggregation.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "AggregationOperationConfiguration")]
public sealed class PipelineTransformAggregationConfigurationCommand : ConfigurationCommandBase<PipelineTransformAggregationConfiguration>
{
    /// <summary>Initializes the command targeting the AggregationOperationConfiguration table.</summary>
    public PipelineTransformAggregationConfigurationCommand() : base("AggregationOperationConfiguration") { }
}
