using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the checks of a pipeline Validate transform.
/// Targets the pipe.PipelineTransformCheck table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save and cascade-read resolve the child command for a
/// <see cref="PipelineTransformCheckConfiguration"/> item via <c>ConfigurationCommands.All()</c>; without this TypeOption
/// a pipeline that has a Validate stage fails to load with NoChildCommandForType (the FDW-1130 defect).
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformCheck")]
public sealed class PipelineTransformCheckConfigurationCommand : ConfigurationCommandBase<PipelineTransformCheckConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineTransformCheck table.</summary>
    public PipelineTransformCheckConfigurationCommand() : base("PipelineTransformCheck") { }
}
