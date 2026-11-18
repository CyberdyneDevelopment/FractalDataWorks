using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for a data store's working-area rows (FDW-6394).</summary>
[TypeOption(typeof(ConfigurationCommands), "DataStoreWorkingPath")]
public sealed class DataStoreWorkingPathConfigurationCommand : ConfigurationCommandBase<DataStoreWorkingPathConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreWorkingPathConfigurationCommand"/> class.</summary>
    public DataStoreWorkingPathConfigurationCommand() : base("DataStoreWorkingPath") { }
}
