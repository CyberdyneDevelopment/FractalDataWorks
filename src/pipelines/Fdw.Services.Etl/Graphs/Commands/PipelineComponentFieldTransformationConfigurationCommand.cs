using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the chain of a component's field mapping. Targets the pipe.PipelineComponentFieldTransformation table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save and cascade-read resolve the command for a <see cref="PipelineComponentFieldTransformationConfiguration"/>
/// item via <c>ConfigurationCommands.All()</c>; without this TypeOption a graph with a chained Map fails to load.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentFieldTransformation")]
public sealed class PipelineComponentFieldTransformationConfigurationCommand : ConfigurationCommandBase<PipelineComponentFieldTransformationConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentFieldTransformation table.</summary>
    public PipelineComponentFieldTransformationConfigurationCommand() : base("PipelineComponentFieldTransformation") { }
}
