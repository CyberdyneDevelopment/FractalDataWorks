using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainerPagingSetting configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataContainerPagingSetting")]
public sealed class DataContainerPagingSettingConfigurationCommand : ConfigurationCommandBase<DataContainerPagingSettingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerPagingSettingConfigurationCommand"/> class.</summary>
    public DataContainerPagingSettingConfigurationCommand() : base("DataContainerPagingSetting") { }
}
