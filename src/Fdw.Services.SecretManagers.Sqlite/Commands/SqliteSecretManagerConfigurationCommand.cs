using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.Sqlite.Configuration;

namespace Fdw.Services.SecretManagers.Sqlite.Commands;

/// <summary>ConfigurationCommands TypeOption for the SqliteSecretManager configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqliteSecretManager")]
public sealed class SqliteSecretManagerConfigurationCommand : ConfigurationCommandBase<SqliteSecretManagerConfiguration>
{
    /// <inheritdoc/>
    public SqliteSecretManagerConfigurationCommand() : base("SqliteSecretManager") { }
}
