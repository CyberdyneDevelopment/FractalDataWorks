using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline transform field mappings.
/// Targets the pipe.PipelineTransformFieldMapping table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformFieldMappingConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting a mapping.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformFieldMapping")]
public sealed class PipelineTransformFieldMappingConfigurationCommand : ConfigurationCommandBase<PipelineTransformFieldMappingConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformFieldMapping table.</summary>
    public PipelineTransformFieldMappingConfigurationCommand() : base("PipelineTransformFieldMapping") { }
}
