using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationLevel child configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "EscalationLevel")]
public sealed class EscalationLevelConfigurationCommand : ConfigurationCommandBase<EscalationLevelConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationLevelConfigurationCommand"/> class.</summary>
    public EscalationLevelConfigurationCommand() : base("EscalationLevel") { }
}
