using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the SQL Server endpoint domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqlServerEndpoint")]
public sealed class SqlServerEndpointConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public SqlServerEndpointConfigurationCommand() : base("SqlServerEndpoint") { }
}
