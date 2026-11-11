using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Commands;

/// <summary>ConfigurationCommands TypeOption for the ExternalIdentityProvisioner configuration domain (sec.ExternalIdentityProvisioner).</summary>
[TypeOption(typeof(ConfigurationCommands), "ExternalIdentityProvisioner")]
public sealed class ExternalIdentityProvisionerConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerConfigurationCommand"/> class.</summary>
    public ExternalIdentityProvisionerConfigurationCommand() : base("ExternalIdentityProvisioner") { }
}
