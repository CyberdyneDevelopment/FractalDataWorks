using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainer configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataContainer")]
public sealed class DataContainerConfigurationCommand : ConfigurationCommandBase<DataContainerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerConfigurationCommand"/> class.</summary>
    public DataContainerConfigurationCommand() : base("DataContainer") { }
}
