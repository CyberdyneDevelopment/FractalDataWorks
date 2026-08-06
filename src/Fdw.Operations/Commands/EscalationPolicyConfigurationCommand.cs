using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationPolicy configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EscalationPolicy")]
public sealed class EscalationPolicyConfigurationCommand : ConfigurationCommandBase<EscalationPolicyConfiguration>
{
    /// <inheritdoc/>
    public EscalationPolicyConfigurationCommand() : base("EscalationPolicy") { }
}
