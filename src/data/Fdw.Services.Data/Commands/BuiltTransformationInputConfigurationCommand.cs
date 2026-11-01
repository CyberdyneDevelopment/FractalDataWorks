using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.BuiltTransformationConfigurationInput</c> rows as <see cref="BuiltTransformationInputConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationInput")]
public sealed class BuiltTransformationInputConfigurationCommand : ConfigurationCommandBase<BuiltTransformationInputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationInputConfigurationCommand"/> class.</summary>
    public BuiltTransformationInputConfigurationCommand() : base("BuiltTransformationConfigurationInput") { }
}
