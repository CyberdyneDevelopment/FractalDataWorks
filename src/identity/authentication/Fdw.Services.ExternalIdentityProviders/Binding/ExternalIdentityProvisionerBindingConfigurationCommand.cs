using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>ConfigurationCommands TypeOption for the ExternalIdentityProvisionerBinding configuration domain (sec.ExternalIdentityProvisionerBinding).</summary>
[TypeOption(typeof(ConfigurationCommands), "ExternalIdentityProvisionerBinding")]
public sealed class ExternalIdentityProvisionerBindingConfigurationCommand : ConfigurationCommandBase<ExternalIdentityProvisionerBindingImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerBindingConfigurationCommand"/> class.</summary>
    public ExternalIdentityProvisionerBindingConfigurationCommand() : base("ExternalIdentityProvisionerBinding") { }
}
