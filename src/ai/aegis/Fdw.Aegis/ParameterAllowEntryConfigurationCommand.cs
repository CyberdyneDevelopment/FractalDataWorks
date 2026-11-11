using Fdw.Aegis.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Aegis;

/// <summary>ConfigurationCommands TypeOption naming the container a PreApproved command's allow-list entries live in.</summary>
[TypeOption(typeof(ConfigurationCommands), "ParameterAllowEntryConfiguration")]
public sealed class ParameterAllowEntryConfigurationCommand : ConfigurationCommandBase<ParameterAllowEntryConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ParameterAllowEntryConfigurationCommand"/> class.</summary>
    public ParameterAllowEntryConfigurationCommand() : base("ParameterAllowEntryConfiguration") { }
}
