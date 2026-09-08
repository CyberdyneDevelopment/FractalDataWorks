using Fdw.Services.Abstractions;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Resolves configured credential services and routes each to the implementation provider that owns it.
/// </summary>
public interface ICredentialServiceConfigurationProvider
    : IDomainConfigurationProvider<ICredentialServiceImplementationConfiguration>
{
}
