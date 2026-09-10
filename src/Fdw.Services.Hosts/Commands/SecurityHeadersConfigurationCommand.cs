using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>ConfigurationCommands TypeOption for the SecurityHeaders domain records.</summary>
[TypeOption(typeof(ConfigurationCommands), "SecurityHeaders")]
public sealed class SecurityHeadersConfigurationCommand : ConfigurationCommandBase<SecurityHeadersConfiguration>
{
    /// <inheritdoc/>
    public SecurityHeadersConfigurationCommand() : base("SecurityHeaders") { }
}
