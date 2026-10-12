using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationTableOutputColumn</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationTableOutputColumn")]
public sealed class BuiltTransformationTableOutputColumnConfigurationCommand : ConfigurationCommandBase<BuiltTransformationTableOutputColumnConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableOutputColumnConfigurationCommand"/> class.</summary>
    public BuiltTransformationTableOutputColumnConfigurationCommand() : base("BuiltTransformationConfigurationTableOutputColumn") { }
}
