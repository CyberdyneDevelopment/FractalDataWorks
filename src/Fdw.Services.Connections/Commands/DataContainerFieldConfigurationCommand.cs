using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainerField configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataContainerField")]
public sealed class DataContainerFieldConfigurationCommand : ConfigurationCommandBase<DataContainerFieldConfiguration>
{
    /// <inheritdoc/>
    public DataContainerFieldConfigurationCommand() : base("DataContainerField") { }
}
