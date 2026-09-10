using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;


namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the data gateway domain record.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataGateway")]
public sealed class DataGatewayDomainConfigurationCommand
    : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public DataGatewayDomainConfigurationCommand() : base("DataGateway") { }
}
