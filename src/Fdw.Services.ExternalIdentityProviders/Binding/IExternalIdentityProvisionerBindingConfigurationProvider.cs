using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the configured ExternalIdentityProvisionerBinding members.</summary>
public interface IExternalIdentityProvisionerBindingConfigurationProvider
    : IDomainConfigurationProvider<IExternalIdentityProvisionerBindingImplementationConfiguration>
{
}
