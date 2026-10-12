using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline transform sort fields.
/// Targets the pipe.PipelineTransformSortField table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformSortFieldConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting a sort field.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformSortField")]
public sealed class PipelineTransformSortFieldConfigurationCommand : ConfigurationCommandBase<PipelineTransformSortFieldConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformSortField table.</summary>
    public PipelineTransformSortFieldConfigurationCommand() : base("PipelineTransformSortField") { }
}
