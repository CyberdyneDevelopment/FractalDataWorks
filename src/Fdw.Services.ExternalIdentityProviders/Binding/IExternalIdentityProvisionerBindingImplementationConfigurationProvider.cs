using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentityProvisionerBinding implementation's own configuration to the domain that registers it.</summary>
public interface IExternalIdentityProvisionerBindingImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IExternalIdentityProvisionerBindingImplementationConfiguration>
{
}
