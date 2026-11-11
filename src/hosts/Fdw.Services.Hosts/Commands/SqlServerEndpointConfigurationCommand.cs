using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the SQL Server endpoint domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqlServerEndpoint")]
public sealed class SqlServerEndpointConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointConfigurationCommand"/> class.</summary>
    public SqlServerEndpointConfigurationCommand() : base("SqlServerEndpoint") { }
}
