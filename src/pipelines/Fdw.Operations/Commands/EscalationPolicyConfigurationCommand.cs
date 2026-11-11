using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>ConfigurationCommands TypeOption for the EscalationPolicy configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EscalationPolicy")]
public sealed class EscalationPolicyConfigurationCommand : ConfigurationCommandBase<EscalationPolicyImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationPolicyConfigurationCommand"/> class.</summary>
    public EscalationPolicyConfigurationCommand() : base("EscalationPolicy") { }
}
