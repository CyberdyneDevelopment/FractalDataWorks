using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationLevelRecipient grandchild configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "EscalationLevelRecipient")]
public sealed class EscalationLevelRecipientConfigurationCommand : ConfigurationCommandBase<EscalationLevelRecipientConfiguration>
{
    /// <inheritdoc/>
    public EscalationLevelRecipientConfigurationCommand() : base("EscalationLevelRecipient") { }
}
