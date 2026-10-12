using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a load step's or value source's columns.
/// Targets the pipe.PipelineLoadColumn table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade resolves the child command for a
/// <see cref="PipelineLoadColumnConfiguration"/> item via <c>ConfigurationCommands.All()</c>; without this TypeOption
/// reading or saving a pipeline's load steps and sequence ordering columns fails loud (NoChildCommandForType) and the pipeline cannot load
/// (FDW-1130).
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineLoadColumn")]
public sealed class PipelineLoadColumnConfigurationCommand : ConfigurationCommandBase<PipelineLoadColumnConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineLoadColumn table.</summary>
    public PipelineLoadColumnConfigurationCommand() : base("PipelineLoadColumn") { }
}
