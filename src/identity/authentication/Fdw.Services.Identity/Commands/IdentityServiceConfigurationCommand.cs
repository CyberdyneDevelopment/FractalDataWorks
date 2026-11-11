using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Identity.Commands;

/// <summary>ConfigurationCommands TypeOption for the Identity configuration domain (sec.Identity).</summary>
[TypeOption(typeof(ConfigurationCommands), "Identity")]
public sealed class IdentityServiceConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="IdentityServiceConfigurationCommand"/> class.</summary>
    public IdentityServiceConfigurationCommand() : base("Identity") { }
}
