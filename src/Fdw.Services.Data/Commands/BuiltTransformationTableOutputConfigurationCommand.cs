using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationTableOutput</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationTableOutput")]
public sealed class BuiltTransformationTableOutputConfigurationCommand : ConfigurationCommandBase<BuiltTransformationTableOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableOutputConfigurationCommand"/> class.</summary>
    public BuiltTransformationTableOutputConfigurationCommand() : base("BuiltTransformationConfigurationTableOutput") { }
}
