using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentity implementation's own configuration.</summary>
public interface IExternalIdentityImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IExternalIdentityImplementationConfiguration>
{
}
