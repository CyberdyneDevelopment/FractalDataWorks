using Fdw.Aegis.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Aegis;

/// <summary>ConfigurationCommands TypeOption naming the container a PreApproved command's allow-list entries live in.</summary>
[TypeOption(typeof(ConfigurationCommands), "ParameterAllowEntryConfiguration")]
public sealed class ParameterAllowEntryConfigurationCommand : ConfigurationCommandBase<ParameterAllowEntryConfiguration>
{
    /// <inheritdoc/>
    public ParameterAllowEntryConfigurationCommand() : base("ParameterAllowEntryConfiguration") { }
}
