using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the data gateway's own configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataGateway")]
public sealed class DataGatewayConfigurationCommand : ConfigurationCommandBase<DataGatewayConfiguration>
{
    /// <inheritdoc/>
    public DataGatewayConfigurationCommand() : base("DataGateway") { }
}
