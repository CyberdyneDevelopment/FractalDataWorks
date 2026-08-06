using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>ConfigurationCommands TypeOption for the ExternalIdentityProvisionerBinding configuration domain (sec.ExternalIdentityProvisionerBinding).</summary>
[TypeOption(typeof(ConfigurationCommands), "ExternalIdentityProvisionerBinding")]
public sealed class ExternalIdentityProvisionerBindingConfigurationCommand : ConfigurationCommandBase<ExternalIdentityProvisionerBindingConfiguration>
{
    /// <inheritdoc/>
    public ExternalIdentityProvisionerBindingConfigurationCommand() : base("ExternalIdentityProvisionerBinding") { }
}
