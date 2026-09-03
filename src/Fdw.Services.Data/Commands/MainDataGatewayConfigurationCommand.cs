using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;


namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the data gateway's typed body.</summary>
[TypeOption(typeof(ConfigurationCommands), "MainDataGateway")]
public sealed class MainDataGatewayConfigurationCommand
    : ConfigurationCommandBase<MainDataGatewayConfiguration>
{
    /// <inheritdoc/>
    public MainDataGatewayConfigurationCommand() : base("MainDataGateway") { }
}
