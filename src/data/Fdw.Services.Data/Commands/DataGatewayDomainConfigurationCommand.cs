using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;


namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the data gateway domain record.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataGateway")]
public sealed class DataGatewayDomainConfigurationCommand
    : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataGatewayDomainConfigurationCommand"/> class.</summary>
    public DataGatewayDomainConfigurationCommand() : base("DataGateway") { }
}
