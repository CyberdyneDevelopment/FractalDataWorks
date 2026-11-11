using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the ConnectionAssignment configuration domain (FDW-1581).</summary>
[TypeOption(typeof(ConfigurationCommands), "ConnectionAssignment")]
public sealed class ConnectionAssignmentConfigurationCommand : ConfigurationCommandBase<ConnectionAssignmentImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionAssignmentConfigurationCommand"/> class.</summary>
    public ConnectionAssignmentConfigurationCommand() : base("ConnectionAssignment") { }
}
