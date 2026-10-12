using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a pipeline's load steps.
/// Targets the pipe.PipelineLoadStep table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade resolves the child command for a
/// <see cref="PipelineLoadStepConfiguration"/> item via <c>ConfigurationCommands.All()</c>; without this TypeOption
/// reading or saving a pipeline's load steps fails loud (NoChildCommandForType) and the pipeline cannot load
/// (FDW-1130).
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineLoadStep")]
public sealed class PipelineLoadStepConfigurationCommand : ConfigurationCommandBase<PipelineLoadStepConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineLoadStep table.</summary>
    public PipelineLoadStepConfigurationCommand() : base("PipelineLoadStep") { }
}
