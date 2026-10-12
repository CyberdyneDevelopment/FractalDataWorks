using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the table rows of a Compute stage. Targets the pipe.PipelineTransformComputeTable table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a <see cref="PipelineTransformComputeTableConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a Compute stage.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformComputeTable")]
public sealed class PipelineTransformComputeTableConfigurationCommand : ConfigurationCommandBase<PipelineTransformComputeTableConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformComputeTable table.</summary>
    public PipelineTransformComputeTableConfigurationCommand() : base("PipelineTransformComputeTable") { }
}
