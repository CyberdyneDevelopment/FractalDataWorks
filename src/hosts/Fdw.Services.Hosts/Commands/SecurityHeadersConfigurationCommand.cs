using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the SecurityHeaders domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "SecurityHeaders")]
public sealed class SecurityHeadersConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersConfigurationCommand"/> class.</summary>
    public SecurityHeadersConfigurationCommand() : base("SecurityHeaders") { }
}
