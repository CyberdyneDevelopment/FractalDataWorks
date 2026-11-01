using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the ConnectionGrant configuration domain (FDW-1581).</summary>
[TypeOption(typeof(ConfigurationCommands), "ConnectionGrant")]
public sealed class ConnectionGrantConfigurationCommand : ConfigurationCommandBase<ConnectionGrantImplementationConfiguration>
{
    /// <inheritdoc/>
    public ConnectionGrantConfigurationCommand() : base("ConnectionGrant") { }
}
