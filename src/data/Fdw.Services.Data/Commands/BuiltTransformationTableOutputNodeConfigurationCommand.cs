using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationTableOutputNode</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationTableOutputNode")]
public sealed class BuiltTransformationTableOutputNodeConfigurationCommand : ConfigurationCommandBase<BuiltTransformationTableOutputNodeConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableOutputNodeConfigurationCommand"/> class.</summary>
    public BuiltTransformationTableOutputNodeConfigurationCommand() : base("BuiltTransformationConfigurationTableOutputNode") { }
}
