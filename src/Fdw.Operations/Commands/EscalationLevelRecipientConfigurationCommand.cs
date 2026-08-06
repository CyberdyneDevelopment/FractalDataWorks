using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationLevelRecipient grandchild configuration.</summary>
// Why: enables the Level→Recipients leg of the keystone save cascade. Create() returns a
// ConfigurationSaveCommand whose translator resolves the physical EscalationLevelRowId FK by subquery
// from the logical EscalationLevelId set by the cascade.
[TypeOption(typeof(ConfigurationCommands), "EscalationLevelRecipient")]
public sealed class EscalationLevelRecipientConfigurationCommand : ConfigurationCommandBase<EscalationLevelRecipientConfiguration>
{
    /// <inheritdoc/>
    public EscalationLevelRecipientConfigurationCommand() : base("EscalationLevelRecipient") { }
}
