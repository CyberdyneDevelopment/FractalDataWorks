using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface IClientCredentialsConfigurationProvider
    : IImplementationConfigurationProvider<IIdentityServiceImplementationConfiguration>
{
}
