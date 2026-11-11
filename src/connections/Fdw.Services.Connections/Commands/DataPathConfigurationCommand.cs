using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataPath configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataPath")]
public sealed class DataPathConfigurationCommand : ConfigurationCommandBase<DataPathConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataPathConfigurationCommand"/> class.</summary>
    public DataPathConfigurationCommand() : base("DataPath") { }
}
