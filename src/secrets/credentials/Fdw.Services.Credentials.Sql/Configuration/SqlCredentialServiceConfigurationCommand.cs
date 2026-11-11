using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// ConfigurationCommands TypeOption for the SqlCredentialService typed body.
/// Produces IDataCommand instances against the SqlCredentialService configuration table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "SqlCredentialServiceConfiguration")]
public sealed class SqlCredentialServiceConfigurationCommand : ConfigurationCommandBase<SqlCredentialServiceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlCredentialServiceConfigurationCommand"/> class.</summary>
    public SqlCredentialServiceConfigurationCommand() : base("SqlCredentialServiceConfiguration") { }
}
