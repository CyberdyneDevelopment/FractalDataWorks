using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for pipeline transform lookups.
/// Targets the pipe.LookupOperationConfiguration table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the child command for a
/// <see cref="PipelineTransformLookupConfiguration"/> item via <c>ConfigurationCommands.All()</c>;
/// without this TypeOption the cascade would fail loud (NoChildCommandForType) when persisting a lookup.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "LookupOperationConfiguration")]
public sealed class PipelineTransformLookupConfigurationCommand : ConfigurationCommandBase<PipelineTransformLookupConfiguration>
{
    /// <summary>Initializes the command targeting the LookupOperationConfiguration table.</summary>
    public PipelineTransformLookupConfigurationCommand() : base("LookupOperationConfiguration") { }
}
