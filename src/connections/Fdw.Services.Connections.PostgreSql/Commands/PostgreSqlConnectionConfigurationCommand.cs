using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.PostgreSql.Commands;

/// <summary>ConfigurationCommands TypeOption for the PostgreSqlConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "PostgreSqlConnectionConfiguration")]
public sealed class PostgreSqlConnectionConfigurationCommand : ConfigurationCommandBase<PostgreSqlConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlConnectionConfigurationCommand"/> class.</summary>
    public PostgreSqlConnectionConfigurationCommand() : base("PostgreSqlConnectionConfiguration") { }
}
