using Fdw.Aegis.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Aegis;

/// <summary>ConfigurationCommands TypeOption naming the container an allow-list entry's permitted values live in.</summary>
[TypeOption(typeof(ConfigurationCommands), "PermittedValueConfiguration")]
public sealed class PermittedValueConfigurationCommand : ConfigurationCommandBase<PermittedValueConfiguration>
{
    /// <inheritdoc/>
    public PermittedValueConfigurationCommand() : base("PermittedValueConfiguration") { }
}
