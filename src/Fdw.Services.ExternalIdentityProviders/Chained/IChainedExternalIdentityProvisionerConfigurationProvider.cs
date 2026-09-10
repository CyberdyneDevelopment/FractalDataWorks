using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface IChainedExternalIdentityProvisionerConfigurationProvider
    : IImplementationConfigurationProvider<IExternalIdentityProvisionerImplementationConfiguration>
{
}
