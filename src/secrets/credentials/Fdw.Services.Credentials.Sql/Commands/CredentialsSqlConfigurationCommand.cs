using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Configuration;

namespace Fdw.Services.Credentials.Sql.Commands;

/// <summary>ConfigurationCommands TypeOption for the SQL credential service selector.</summary>
[TypeOption(typeof(ConfigurationCommands), "CredentialsSqlConfiguration")]
public sealed class CredentialsSqlConfigurationCommand : ConfigurationCommandBase<CredentialsSqlConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CredentialsSqlConfigurationCommand"/> class.</summary>
    public CredentialsSqlConfigurationCommand() : base("CredentialsSqlConfiguration") { }
}
