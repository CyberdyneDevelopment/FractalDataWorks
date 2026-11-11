using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// ConfigurationCommands TypeOption for the ClaimMappedExternalIdentityProvisioner typed-body domain.
/// Routes configuration save/delete operations for
/// <see cref="ClaimMappedExternalIdentityProvisionerConfiguration"/> (the ClaimMapped typed-body
/// record in <c>sec.ClaimMappedExternalIdentityProvisioner</c>).
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "ClaimMappedExternalIdentityProvisionerConfiguration")]
public sealed class ClaimMappedExternalIdentityProvisionerConfigurationCommand
    : ConfigurationCommandBase<ClaimMappedExternalIdentityProvisionerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ClaimMappedExternalIdentityProvisionerConfigurationCommand"/> class.</summary>
    public ClaimMappedExternalIdentityProvisionerConfigurationCommand()
        : base("ClaimMappedExternalIdentityProvisionerConfiguration") { }
}
