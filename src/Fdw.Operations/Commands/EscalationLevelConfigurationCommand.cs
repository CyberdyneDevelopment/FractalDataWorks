using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationLevel child configuration.</summary>
// Why: the keystone save cascade (ImplementationConfigurationProviderBase.CascadeChildSave → SaveOneChild) resolves
// each child's command by ConfigType from ConfigurationCommands.All(). Without this TypeOption the
// Policy→Levels cascade would fail loud (NoChildCommandForType). Create() returns a ConfigurationSaveCommand
// whose translator resolves the physical EscalationPolicyRowId FK by subquery from the logical EscalationPolicyId.
[TypeOption(typeof(ConfigurationCommands), "EscalationLevel")]
public sealed class EscalationLevelConfigurationCommand : ConfigurationCommandBase<EscalationLevelConfiguration>
{
    /// <inheritdoc/>
    public EscalationLevelConfigurationCommand() : base("EscalationLevel") { }
}
