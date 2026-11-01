using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationTableInput</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationTableInput")]
public sealed class BuiltTransformationTableInputConfigurationCommand : ConfigurationCommandBase<BuiltTransformationTableInputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableInputConfigurationCommand"/> class.</summary>
    public BuiltTransformationTableInputConfigurationCommand() : base("BuiltTransformationConfigurationTableInput") { }
}
