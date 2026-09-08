using Fdw.Services.Abstractions;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Resolves configured identity services and routes each to the implementation provider that owns it.
/// </summary>
public interface IIdentityServiceConfigurationProvider
    : IDomainConfigurationProvider<IIdentityServiceImplementationConfiguration>
{
}
