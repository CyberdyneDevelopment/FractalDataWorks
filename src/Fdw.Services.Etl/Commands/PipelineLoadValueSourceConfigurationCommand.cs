using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a load step's value sources.
/// Targets the pipe.PipelineLoadValueSource table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade resolves the child command for a
/// <see cref="PipelineLoadValueSourceConfiguration"/> item via <c>ConfigurationCommands.All()</c>; without this TypeOption
/// reading or saving a pipeline's load steps fails loud (NoChildCommandForType) and the pipeline cannot load
/// (FDW-1130).
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineLoadValueSource")]
public sealed class PipelineLoadValueSourceConfigurationCommand : ConfigurationCommandBase<PipelineLoadValueSourceConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineLoadValueSource table.</summary>
    public PipelineLoadValueSourceConfigurationCommand() : base("PipelineLoadValueSource") { }
}
