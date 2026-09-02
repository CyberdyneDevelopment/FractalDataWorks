using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Configuration;

namespace Fdw.Services.Credentials.Sql.Commands;

/// <summary>ConfigurationCommands TypeOption for the SQL credential service selector.</summary>
[TypeOption(typeof(ConfigurationCommands), "CredentialsSql")]
public sealed class CredentialsSqlConfigurationCommand : ConfigurationCommandBase<CredentialsSqlConfiguration>
{
    /// <inheritdoc/>
    public CredentialsSqlConfigurationCommand() : base("CredentialsSql") { }
}
