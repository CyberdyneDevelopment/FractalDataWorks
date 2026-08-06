using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// ConfigurationCommands TypeOption for the SqlCredentialService typed body.
/// Produces IDataCommand instances against the SqlCredentialService configuration table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "SqlCredentialService")]
public sealed class SqlCredentialServiceConfigurationCommand : ConfigurationCommandBase<SqlCredentialServiceConfiguration>
{
    /// <inheritdoc/>
    public SqlCredentialServiceConfigurationCommand() : base("SqlCredentialService") { }
}
