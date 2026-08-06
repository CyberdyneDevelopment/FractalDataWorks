using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataStore configuration domain.</summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataStore")]
public sealed class DataStoreConfigurationCommand : ConfigurationCommandBase<DataStoreConfiguration>
{
    /// <inheritdoc/>
    public DataStoreConfigurationCommand() : base("DataStore") { }
}
