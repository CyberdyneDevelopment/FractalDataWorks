using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the OutboundIdentity domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "OutboundIdentity")]
public sealed class OutboundIdentityConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public OutboundIdentityConfigurationCommand() : base("OutboundIdentity") { }
}
