using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.MsSql.Commands;

/// <summary>ConfigurationCommands TypeOption for the MsSqlConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "MsSqlConnectionConfiguration")]
public sealed class MsSqlConnectionConfigurationCommand : ConfigurationCommandBase<MsSqlConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlConnectionConfigurationCommand"/> class.</summary>
    public MsSqlConnectionConfigurationCommand() : base("MsSqlConnectionConfiguration") { }
}
