using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Sqlite.Commands;

/// <summary>ConfigurationCommands TypeOption for the SqliteConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "SqliteConnection")]
public sealed class SqliteConnectionConfigurationCommand : ConfigurationCommandBase<SqliteConnectionConfiguration>
{
    /// <inheritdoc/>
    public SqliteConnectionConfigurationCommand() : base("SqliteConnection") { }
}
