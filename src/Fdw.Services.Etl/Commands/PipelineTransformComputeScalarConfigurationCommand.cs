using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the scalar rows of a Compute stage. Targets the pipe.PipelineTransformComputeScalar table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a <see cref="PipelineTransformComputeScalarConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a Compute stage.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformComputeScalar")]
public sealed class PipelineTransformComputeScalarConfigurationCommand : ConfigurationCommandBase<PipelineTransformComputeScalarConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformComputeScalar table.</summary>
    public PipelineTransformComputeScalarConfigurationCommand() : base("PipelineTransformComputeScalar") { }
}
