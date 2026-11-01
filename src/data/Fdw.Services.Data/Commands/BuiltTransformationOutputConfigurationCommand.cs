using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.BuiltTransformationConfigurationOutput</c> rows as <see cref="BuiltTransformationOutputConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "BuiltTransformationConfigurationOutput")]
public sealed class BuiltTransformationOutputConfigurationCommand : ConfigurationCommandBase<BuiltTransformationOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationOutputConfigurationCommand"/> class.</summary>
    public BuiltTransformationOutputConfigurationCommand() : base("BuiltTransformationConfigurationOutput") { }
}
