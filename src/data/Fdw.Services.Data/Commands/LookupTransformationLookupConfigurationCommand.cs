using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.LookupTransformationConfigurationLookup</c> rows as <see cref="LookupTransformationLookupConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "LookupTransformationConfigurationLookup")]
public sealed class LookupTransformationLookupConfigurationCommand : ConfigurationCommandBase<LookupTransformationLookupConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LookupTransformationLookupConfigurationCommand"/> class.</summary>
    public LookupTransformationLookupConfigurationCommand() : base("LookupTransformationConfigurationLookup") { }
}
