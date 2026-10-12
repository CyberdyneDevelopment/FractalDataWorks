using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface IClaimMappedExternalIdentityProvisionerConfigurationProvider
    : IImplementationConfigurationProvider<IExternalIdentityProvisionerImplementationConfiguration>
{
}
