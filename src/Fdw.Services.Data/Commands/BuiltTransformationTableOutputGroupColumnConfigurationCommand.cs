using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationTableOutputGroupColumn</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationTableOutputGroupColumn")]
public sealed class BuiltTransformationTableOutputGroupColumnConfigurationCommand : ConfigurationCommandBase<BuiltTransformationTableOutputGroupColumnConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableOutputGroupColumnConfigurationCommand"/> class.</summary>
    public BuiltTransformationTableOutputGroupColumnConfigurationCommand() : base("BuiltTransformationConfigurationTableOutputGroupColumn") { }
}
