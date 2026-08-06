using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.MsSql.Commands;

/// <summary>ConfigurationCommands TypeOption for the MsSqlConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "MsSqlConnection")]
public sealed class MsSqlConnectionConfigurationCommand : ConfigurationCommandBase<MsSqlConnectionConfiguration>
{
    /// <inheritdoc/>
    public MsSqlConnectionConfigurationCommand() : base("MsSqlConnection") { }
}
