using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline transform aggregate group-by fields.
/// Targets the pipe.AggregationGroupByField table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformGroupByFieldConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting a group-by field.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "AggregationGroupByField")]
public sealed class PipelineTransformGroupByFieldConfigurationCommand : ConfigurationCommandBase<PipelineTransformGroupByFieldConfiguration>
{
    /// <summary>Initializes the command targeting the AggregationGroupByField table.</summary>
    public PipelineTransformGroupByFieldConfigurationCommand() : base("AggregationGroupByField") { }
}
