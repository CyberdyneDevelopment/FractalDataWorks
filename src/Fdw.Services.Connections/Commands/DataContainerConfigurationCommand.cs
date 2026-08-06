using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainer configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataContainer")]
public sealed class DataContainerConfigurationCommand : ConfigurationCommandBase<DataContainerConfiguration>
{
    /// <inheritdoc/>
    public DataContainerConfigurationCommand() : base("DataContainer") { }
}
