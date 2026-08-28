using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationLevel child configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "EscalationLevel")]
public sealed class EscalationLevelConfigurationCommand : ConfigurationCommandBase<EscalationLevelConfiguration>
{
    /// <inheritdoc/>
    public EscalationLevelConfigurationCommand() : base("EscalationLevel") { }
}
