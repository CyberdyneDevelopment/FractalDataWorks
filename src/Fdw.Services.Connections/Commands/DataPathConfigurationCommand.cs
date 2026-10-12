using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataPath configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataPath")]
public sealed class DataPathConfigurationCommand : ConfigurationCommandBase<DataPathConfiguration>
{
    /// <inheritdoc/>
    public DataPathConfigurationCommand() : base("DataPath") { }
}
