using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// The configuration command of a Branch stage's path, which declares the table the path's rows live in.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "PipelineTransformPath")]
public sealed class PipelineTransformPathConfigurationCommand : ConfigurationCommandBase<PipelineTransformPathConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTransformPathConfigurationCommand"/> class.</summary>
    public PipelineTransformPathConfigurationCommand() : base("PipelineTransformPath") { }
}
