using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentity domain configuration.</summary>
public interface IExternalIdentityConfigurationProvider
    : IDomainConfigurationProvider<IExternalIdentityImplementationConfiguration>
{
}
