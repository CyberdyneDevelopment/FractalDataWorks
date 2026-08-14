using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.Commands;

/// <summary>ConfigurationCommands TypeOption for the Identity configuration domain (sec.Identity).</summary>
[TypeOption(typeof(ConfigurationCommands), "Identity")]
public sealed class IdentityServiceConfigurationCommand : ConfigurationCommandBase<IdentityServiceConfiguration>
{
    /// <inheritdoc/>
    public IdentityServiceConfigurationCommand() : base("Identity") { }
}
