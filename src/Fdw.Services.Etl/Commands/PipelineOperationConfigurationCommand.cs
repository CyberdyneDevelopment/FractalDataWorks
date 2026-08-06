using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline operations (transform steps).
/// Targets the pipe.PipelineOperation table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformConfiguration"/> item via <c>ConfigurationCommands.All()</c>; without
/// this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting an operation.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineOperation")]
public sealed class PipelineOperationConfigurationCommand : ConfigurationCommandBase<PipelineTransformConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineOperation table.</summary>
    public PipelineOperationConfigurationCommand() : base("PipelineOperation") { }
}
