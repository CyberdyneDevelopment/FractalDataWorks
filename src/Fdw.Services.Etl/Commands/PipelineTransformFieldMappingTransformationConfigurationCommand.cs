using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the transformer chain of a pipeline field mapping.
/// Targets the pipe.PipelineTransformFieldMappingTransformation table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save and cascade-read resolve the child command for a
/// <see cref="PipelineTransformFieldMappingTransformationConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption a pipeline with a Map stage fails to load with NoChildCommandForType (the FDW-1130 defect).
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformFieldMappingTransformation")]
public sealed class PipelineTransformFieldMappingTransformationConfigurationCommand : ConfigurationCommandBase<PipelineTransformFieldMappingTransformationConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformFieldMappingTransformation table.</summary>
    public PipelineTransformFieldMappingTransformationConfigurationCommand() : base("PipelineTransformFieldMappingTransformation") { }
}
