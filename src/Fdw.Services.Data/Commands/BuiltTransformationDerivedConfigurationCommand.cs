using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes the <c>transform.BuiltTransformationConfigurationDerived</c> rows (FDW-1231).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationDerived")]
public sealed class BuiltTransformationDerivedConfigurationCommand : ConfigurationCommandBase<BuiltTransformationDerivedConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationDerivedConfigurationCommand"/> class.</summary>
    public BuiltTransformationDerivedConfigurationCommand() : base("BuiltTransformationConfigurationDerived") { }
}
