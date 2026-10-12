using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.BuiltTransformationConfigurationOutputNode</c> rows as <see cref="BuiltTransformationOutputNodeConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationOutputNode")]
public sealed class BuiltTransformationOutputNodeConfigurationCommand : ConfigurationCommandBase<BuiltTransformationOutputNodeConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationOutputNodeConfigurationCommand"/> class.</summary>
    public BuiltTransformationOutputNodeConfigurationCommand() : base("BuiltTransformationConfigurationOutputNode") { }
}
