using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the inputs of a Compute stage.
/// Targets the pipe.PipelineTransformComputeInput table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformComputeInputConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting a Compute stage.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformComputeInput")]
public sealed class PipelineTransformComputeInputConfigurationCommand : ConfigurationCommandBase<PipelineTransformComputeInputConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformComputeInput table.</summary>
    public PipelineTransformComputeInputConfigurationCommand() : base("PipelineTransformComputeInput") { }
}
