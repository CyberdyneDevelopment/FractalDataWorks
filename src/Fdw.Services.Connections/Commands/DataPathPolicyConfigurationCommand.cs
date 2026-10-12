using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataPathPolicy configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataPathPolicy")]
public sealed class DataPathPolicyConfigurationCommand : ConfigurationCommandBase<DataPathPolicyConfiguration>
{
    /// <inheritdoc/>
    public DataPathPolicyConfigurationCommand() : base("DataPathPolicy") { }
}
