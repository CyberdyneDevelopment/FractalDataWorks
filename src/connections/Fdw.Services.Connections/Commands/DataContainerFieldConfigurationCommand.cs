using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainerField configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataContainerField")]
public sealed class DataContainerFieldConfigurationCommand : ConfigurationCommandBase<DataContainerFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerFieldConfigurationCommand"/> class.</summary>
    public DataContainerFieldConfigurationCommand() : base("DataContainerField") { }
}
