using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataPathPolicy configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataPathPolicy")]
public sealed class DataPathPolicyConfigurationCommand : ConfigurationCommandBase<DataPathPolicyConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataPathPolicyConfigurationCommand"/> class.</summary>
    public DataPathPolicyConfigurationCommand() : base("DataPathPolicy") { }
}
