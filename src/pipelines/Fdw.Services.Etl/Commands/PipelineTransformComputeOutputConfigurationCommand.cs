using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the output rows of a Compute stage. Targets the pipe.PipelineTransformComputeOutput table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a <see cref="PipelineTransformComputeOutputConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a Compute stage.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformComputeOutput")]
public sealed class PipelineTransformComputeOutputConfigurationCommand : ConfigurationCommandBase<PipelineTransformComputeOutputConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformComputeOutput table.</summary>
    public PipelineTransformComputeOutputConfigurationCommand() : base("PipelineTransformComputeOutput") { }
}
