using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationTableInputColumn</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationTableInputColumn")]
public sealed class BuiltTransformationTableInputColumnConfigurationCommand : ConfigurationCommandBase<BuiltTransformationTableInputColumnConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableInputColumnConfigurationCommand"/> class.</summary>
    public BuiltTransformationTableInputColumnConfigurationCommand() : base("BuiltTransformationConfigurationTableInputColumn") { }
}
