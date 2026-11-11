using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a field transformation's parameters, the typed-list
/// <c>Parameters</c> child of <see cref="FieldTransformationConfiguration"/>. Targets
/// transform.FieldTransformationConfigurationParameter; without it the transformation's read fails
/// with NoChildCommandForType.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "FieldTransformationConfigurationParameter")]
public sealed class FieldTransformationParameterConfigurationCommand : ConfigurationCommandBase<FieldTransformationParameterConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FieldTransformationParameterConfigurationCommand"/> class.</summary>
    public FieldTransformationParameterConfigurationCommand() : base("FieldTransformationConfigurationParameter") { }
}
