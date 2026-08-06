using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// ConfigurationCommands TypeOption for the ChainedExternalIdentityProvisioner typed-body domain.
/// Routes configuration save/delete operations for
/// <see cref="ChainedExternalIdentityProvisionerConfiguration"/> (the Chained typed-body record in
/// <c>sec.ChainedExternalIdentityProvisioner</c>).
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "ChainedExternalIdentityProvisioner")]
public sealed class ChainedExternalIdentityProvisionerConfigurationCommand
    : ConfigurationCommandBase<ChainedExternalIdentityProvisionerConfiguration>
{
    /// <inheritdoc/>
    public ChainedExternalIdentityProvisionerConfigurationCommand()
        : base("ChainedExternalIdentityProvisioner") { }
}
