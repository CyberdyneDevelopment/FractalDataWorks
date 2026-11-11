using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the Connection configuration domain.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "Connection")]
public sealed class ConnectionConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionConfigurationCommand"/> class.</summary>
    public ConnectionConfigurationCommand() : base("Connection") { }
}
