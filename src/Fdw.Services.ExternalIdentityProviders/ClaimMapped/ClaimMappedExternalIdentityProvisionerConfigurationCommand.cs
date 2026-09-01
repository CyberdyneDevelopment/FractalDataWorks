using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// ConfigurationCommands TypeOption for the ClaimMappedExternalIdentityProvisioner typed-body domain.
/// Routes configuration save/delete operations for
/// <see cref="ClaimMappedExternalIdentityProvisionerConfiguration"/> (the ClaimMapped typed-body
/// record in <c>sec.ClaimMappedExternalIdentityProvisioner</c>).
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "ClaimMappedExternalIdentityProvisioner")]
public sealed class ClaimMappedExternalIdentityProvisionerConfigurationCommand
    : ConfigurationCommandBase<ClaimMappedExternalIdentityProvisionerConfiguration>
{
    /// <inheritdoc/>
    public ClaimMappedExternalIdentityProvisionerConfigurationCommand()
        : base("ClaimMappedExternalIdentityProvisioner") { }
}
