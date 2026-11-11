using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataStore configuration domain.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataStore")]
public sealed class DataStoreConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreConfigurationCommand"/> class.</summary>
    public DataStoreConfigurationCommand() : base("DataStore") { }
}
