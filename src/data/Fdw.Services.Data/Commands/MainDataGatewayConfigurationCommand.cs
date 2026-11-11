using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;


namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the data gateway's typed body.</summary>
[TypeOption(typeof(ConfigurationCommands), "MainDataGatewayConfiguration")]
public sealed class MainDataGatewayConfigurationCommand
    : ConfigurationCommandBase<MainDataGatewayConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MainDataGatewayConfigurationCommand"/> class.</summary>
    public MainDataGatewayConfigurationCommand() : base("MainDataGatewayConfiguration") { }
}
