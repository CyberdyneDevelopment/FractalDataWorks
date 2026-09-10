using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Supplies CORS configuration. Registered and resolved as this type — never as the base.
/// </summary>
public interface ICorsConfigurationProvider
    : IDomainConfigurationProvider<ICorsImplementationConfiguration>
{
}
