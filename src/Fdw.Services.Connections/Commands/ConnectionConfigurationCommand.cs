using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the Connection configuration domain.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "Connection")]
public sealed class ConnectionConfigurationCommand : ConfigurationCommandBase<ConnectionConfiguration>
{
    /// <inheritdoc/>
    public ConnectionConfigurationCommand() : base("Connection") { }
}
